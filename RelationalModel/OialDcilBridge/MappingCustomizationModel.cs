#region Common Public License Copyright Notice
/**************************************************************************\
* Neumont Object-Role Modeling Architect for Visual Studio                 *
*                                                                          *
* Copyright � Neumont University. All rights reserved.                     *
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

using System;
using Neumont.Tools.Modeling;
using Neumont.Tools.ORM.ObjectModel;
using Microsoft.VisualStudio.Modeling;

namespace Neumont.Tools.ORMAbstractionToConceptualDatabaseBridge
{
	partial class MappingCustomizationModel
	{
		#region Static helper functions
		// Put this first so that MappingCustomizationModel.resx binds the resource name to the correct class
		/// <summary>
		/// Find or create the <see cref="MappingCustomizationModel"/> for the given <paramref name="store"/>
		/// </summary>
		/// <param name="store">The context <see cref="Store"/></param>
		/// <param name="forceCreate">Set to <see langword="true"/> to force a new customization model to
		/// be created if one does not already exist.</param>
		public static MappingCustomizationModel GetMappingCustomizationModel(Store store, bool forceCreate)
		{
			MappingCustomizationModel model = null;
			foreach (MappingCustomizationModel findModel in store.ElementDirectory.FindElements<MappingCustomizationModel>())
			{
				model = findModel;
				break;
			}
			if (model == null && forceCreate)
			{
				model = new MappingCustomizationModel(store);
			}
			return model;
		}
		#endregion // Static helper functions
	}
	public partial class ORMAbstractionToConceptualDatabaseBridgeDomainModel : IModelingEventSubscriber
	{
		#region IModelingEventSubscriber Implementation
		/// <summary>
		/// Implements <see cref="IModelingEventSubscriber.ManagePostLoadModelingEventHandlers"/>
		/// </summary>
		protected void ManagePostLoadModelingEventHandlers(ModelingEventManager eventManager, bool isReload, EventHandlerAction action)
		{
			IORMPropertyProviderService propertyProvider = ((IORMToolServices)Store).PropertyProviderService;
			propertyProvider.AddOrRemovePropertyProvider<FactType>(AssimilationMapping.PopulateAssimilationMappingExtensionProperties, true, action);
			propertyProvider.AddOrRemovePropertyProvider<ObjectType>(AssimilationMapping.PopulateObjectTypeAbsorptionExtensionProperties, false, action);
			propertyProvider.AddOrRemovePropertyProvider<ObjectType>(ReferenceModeNaming.PopulateReferenceModeNamingExtensionProperties, false, action);
			propertyProvider.AddOrRemovePropertyProvider<ORMModel>(ReferenceModeNaming.PopulateDefaultReferenceModeNamingExtensionProperties, false, action);
		}
		void IModelingEventSubscriber.ManagePostLoadModelingEventHandlers(ModelingEventManager eventManager, bool isReload, EventHandlerAction action)
		{
			ManagePostLoadModelingEventHandlers(eventManager, isReload, action);
		}
		/// <summary>
		/// Implements <see cref="IModelingEventSubscriber.ManagePreLoadModelingEventHandlers"/>
		/// </summary>
		protected static void ManagePreLoadModelingEventHandlers(ModelingEventManager eventManager, bool isReload, EventHandlerAction action)
		{
			// Nothing to do
		}
		void IModelingEventSubscriber.ManagePreLoadModelingEventHandlers(ModelingEventManager eventManager, bool isReload, EventHandlerAction action)
		{
			ManagePreLoadModelingEventHandlers(eventManager, isReload, action);
		}
		/// <summary>
		/// Implements <see cref="IModelingEventSubscriber.ManageSurveyQuestionModelingEventHandlers"/>
		/// </summary>
		protected static void ManageSurveyQuestionModelingEventHandlers(ModelingEventManager eventManager, bool isReload, EventHandlerAction action)
		{
			// Nothing to do
		}
		void IModelingEventSubscriber.ManageSurveyQuestionModelingEventHandlers(ModelingEventManager eventManager, bool isReload, EventHandlerAction action)
		{
			ManageSurveyQuestionModelingEventHandlers(eventManager, isReload, action);
		}
		#endregion // IModelingEventSubscriber Implementation
	}
}
