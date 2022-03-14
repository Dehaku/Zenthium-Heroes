using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Devdog.General
{
    public partial class GeneralSettingsManager : ManagerBase<GeneralSettingsManager>
    {
        [Required]
        public GeneralSettings settings;


        protected override void Awake()
        {
            base.Awake();

            settings.defaultCursor.Enable();
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.raiseExceptions = settings.useExceptionsForAssertions;
#pragma warning restore CS0618 // Type or member is obsolete
            DevdogLogger.minimaLog = settings.minimalLogType;
        }
    }
}
