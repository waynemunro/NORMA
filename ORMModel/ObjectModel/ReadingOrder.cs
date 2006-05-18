#region Common Public License Copyright Notice
/**************************************************************************\
* Neumont Object-Role Modeling Architect for Visual Studio                 *
*                                                                          *
* Copyright © Neumont University. All rights reserved.                     *
*                                                                          *
* The use and distribution terms for this software are covered by the      *
* Common Public License 1.0 (http://opensource.org/licenses/cpl) which     *
* can be found in the file CPL.txt at the root of this distribution.       *
* By using this software in any fashion, you are agreeing to be bound by   *
* the terms of this license.                                               *
*                                                                          *
* You must not remove this notice, or any other, from this software.       *
\**************************************************************************/
#endregion

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualStudio.Modeling;

#endregion

namespace Neumont.Tools.ORM.ObjectModel
{
	public partial class ReadingOrder : IRedirectVerbalization, IHasIndirectModelErrorOwner
	{
		#region Reading facade method
		/// <summary>
		/// Adds a reading to the fact.
		/// </summary>
		/// <param name="readingText">The text of the reading to add.</param>
		/// <returns>The reading that was added.</returns>
		public Reading AddReading(string readingText)
		{
			RoleBaseMoveableCollection factRoles = RoleCollection;
			int roleCount = factRoles.Count;
			if (!Reading.IsValidReadingText(readingText, roleCount))
			{
				throw new ArgumentException(ResourceStrings.ModelExceptionFactAddReadingInvalidReadingText, "readingText");
			}

			Store store = Store;
			Reading retVal = Reading.CreateAndInitializeReading(
				store,
				new AttributeAssignment[]{
					new AttributeAssignment(Reading.TextMetaAttributeGuid, readingText, store)});
			retVal.ReadingOrder = this;
			return retVal;
		}
		#endregion // Reading facade method
		#region CustomStoredAttribute handling
		/// <summary>
		/// Currently only handles when the ReadingText value is accessed.
		/// </summary>
		public override object GetValueForCustomStoredAttribute(MetaAttributeInfo attribute)
		{
			object retval = null;
			if (attribute.Id == ReadingTextMetaAttributeGuid)
			{
				ReadingMoveableCollection readings = ReadingCollection;
				if (readings.Count == 0)
				{
					retval = String.Empty;
				}
				else
				{
					retval = readings[0].Text;
				}
			}
			else
			{
				retval = base.GetValueForCustomStoredAttribute(attribute);
			}
			return retval;
		}

		/// <summary>
		/// Currently only handles when the ReadingText is set.
		/// </summary>
		public override void SetValueForCustomStoredAttribute(MetaAttributeInfo attribute, object newValue)
		{
			if (attribute.Id == ReadingTextMetaAttributeGuid)
			{
				ReadingMoveableCollection readings = ReadingCollection;
				if (readings.Count > 0)
				{
					readings[0].Text = (string)newValue;
				}
			}
			else
			{
				base.SetValueForCustomStoredAttribute(attribute, newValue);
			}
		}
		#endregion
		#region PrimaryReading property and helpers
		/// <summary>
		/// An alternate means of setting and retrieving which reading is primary.
		/// </summary>
		/// <value>The primary Reading.</value>
		public Reading PrimaryReading
		{
			get
			{
				ReadingMoveableCollection readings;
				if (!IsRemoved &&
					0 != (readings = ReadingCollection).Count)
				{
					return readings[0];
				}
				return null;
			}
		}
		#endregion
		#region EnforceNoEmptyReadingOrder rule class
		[RuleOn(typeof(ReadingOrderHasReading), FireTime = TimeToFire.LocalCommit)]
		private class EnforceNoEmptyReadingOrder : RemoveRule
		{
			/// <summary>
			/// If the ReadingOrder.ReadingCollection is empty then remove the ReadingOrder
			/// </summary>
			/// <param name="e"></param>
			public override void ElementRemoved(ElementRemovedEventArgs e)
			{
				ReadingOrderHasReading link = e.ModelElement as ReadingOrderHasReading;
				ReadingOrder readOrd = link.ReadingOrder;
				if (!readOrd.IsRemoved)
				{
					if (readOrd.ReadingCollection.Count == 0)
					{
						readOrd.Remove();
					}
				}
			}
		}
		#endregion // EnforceNoEmptyReadingOrder rule class
		#region ReadingOrderHasRoleRemoving rule class
		/// <summary>
		/// Handles the clean up of the readings that the role is involved in by replacing
		/// the place holder with the text {{deleted}}
		/// </summary>
		[RuleOn(typeof(ReadingOrderHasRole))]
		private class ReadingOrderHasRoleRemoving : RemovingRule
		{
			//UNDONE:a role being removed creates the possibility of there being two ReadingOrders with the same Role sequences, they should be merged
			
			public override void ElementRemoving(ElementRemovingEventArgs e)
			{
				ReadingOrderHasRole link = e.ModelElement as ReadingOrderHasRole;
				RoleBase linkRole = link.RoleCollection;
				ReadingOrder linkReadingOrder = link.ReadingOrder;

				if (linkReadingOrder.IsRemoving)
				{
					// Don't validate if we're removing the reading order
					return;
				}
				Debug.Assert(!linkReadingOrder.IsRemoved);

				int pos = linkReadingOrder.RoleCollection.IndexOf(linkRole);
				if (pos >= 0)
				{
					// UNDONE: This could be done much cleaner with RegEx.Replace and a callback
					ReadingMoveableCollection readings = linkReadingOrder.ReadingCollection;
					int numReadings = readings.Count;
					int roleCount = linkReadingOrder.RoleCollection.Count;
					for (int iReading = 0; iReading < numReadings; ++iReading)
					{
						Reading linkReading = readings[iReading];

						if (!linkReading.IsRemoving)
						{
							Debug.Assert(!linkReading.IsRemoved);
							string text = linkReading.Text;
							text = text.Replace("{" + pos.ToString(CultureInfo.InvariantCulture) + "}", ResourceStrings.ModelReadingRoleDeletedRoleText);
							for (int i = pos + 1; i < roleCount; ++i)
							{
								text = text.Replace(string.Concat("{", i.ToString(CultureInfo.InvariantCulture), "}"), string.Concat("{", (i - 1).ToString(CultureInfo.InvariantCulture), "}"));
							}
							linkReading.Text = text;
							//UNDONE:add entry to task list service to let user know reading text might need some fixup
						}
					}
				}
			}
		}
		#endregion // ReadingOrderHasRoleRemoving rule class
		#region FactTypeHasRoleAddedRule
		/// <summary>
		/// Common place for code to deal with roles that exist in a fact
		/// but do not exist in the ReadingOrder objects that it contains.
		/// This allows it to be used by both the rule and to be called
		/// during post load model fixup.
		/// </summary>
		private static void ValidateReadingOrdersRoleCollection(FactType theFact, RoleBase addedRole)
		{
			Debug.Assert(theFact.Store.TransactionManager.InTransaction);

			string deletedText = ResourceStrings.ModelReadingRoleDeletedRoleText;
			// TODO: escape the deletedText for any Regex text, since it's localizable
			Regex regExDeleted = new Regex(deletedText, RegexOptions.Compiled);

			ReadingOrderMoveableCollection readingOrders = theFact.ReadingOrderCollection;
			foreach (ReadingOrder ord in readingOrders)
			{
				RoleBaseMoveableCollection roles = ord.RoleCollection;
				if (!roles.Contains(addedRole))
				{
					ord.RoleCollection.Add(addedRole);
					ReadingMoveableCollection readings = ord.ReadingCollection;
					foreach (Reading read in readings)
					{
						string readingText = read.Text;
						
						int pos = readingText.IndexOf(deletedText);
						string newText;
						if (pos < 0)
						{
							newText = String.Concat(readingText, "{", roles.Count - 1, "}");
						}
						else
						{
							newText = regExDeleted.Replace(readingText, string.Concat("{", roles.Count - 1, "}"), 1);
						}
						//UNDONE:add entries to the task list service to let user know the reading might need some correction

						read.Text = newText;
					}
				}
			}
		}

		/// <summary>
		/// Rule to detect when a Role is added to the FactType so that it
		/// can also be added to the ReadingOrders and their Readings.
		/// </summary>
		[RuleOn(typeof(FactTypeHasRole))]
		private class FactTypeHasRoleAddedRule : AddRule
		{
			public override void ElementAdded(ElementAddedEventArgs e)
			{
				FactTypeHasRole link = e.ModelElement as FactTypeHasRole;
				ValidateReadingOrdersRoleCollection(link.FactType, link.RoleCollection);
			}
		}
		#endregion
		#region IRedirectVerbalization Implementation
		/// <summary>
		/// Implements IRedirectVerbalization.SurrogateVerbalizer by deferring to the parent fact
		/// </summary>
		protected IVerbalize SurrogateVerbalizer
		{
			get
			{
				return FactType as IVerbalize;
			}
		}
		IVerbalize IRedirectVerbalization.SurrogateVerbalizer
		{
			get
			{
				return SurrogateVerbalizer;
			}
		}
		#endregion // IRedirectVerbalization Implementation
		#region IHasIndirectModelErrorOwner Implementation
		private static readonly Guid[] myIndirectModelErrorOwnerLinkRoles = new Guid[] { FactTypeHasReadingOrder.ReadingOrderCollectionMetaRoleGuid };
		/// <summary>
		/// Implements IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		/// </summary>
		protected static Guid[] GetIndirectModelErrorOwnerLinkRoles()
		{
			return myIndirectModelErrorOwnerLinkRoles;
		}
		Guid[] IHasIndirectModelErrorOwner.GetIndirectModelErrorOwnerLinkRoles()
		{
			return GetIndirectModelErrorOwnerLinkRoles();
		}
		#endregion // IHasIndirectModelErrorOwner Implementation
	}
}
