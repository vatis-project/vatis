﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Vatsim.Vatis.Weather.Enums;
using Vatsim.Vatis.Weather.Extensions;

namespace Vatsim.Vatis.Weather.Objects
{
    /// <summary>
    /// Information about air pressure
    /// </summary>
    [DataContract(Name = "altimeterSetting")]
    public class AltimeterSetting
    {
        /// <summary>
        /// Altimeter unit type
        /// </summary>
        [DataMember(Name = "unitType", EmitDefaultValue = false)]
        public AltimeterUnitType UnitType { get; init; }

        /// <summary>
        /// Altimeter value
        /// </summary>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public int Value { get; init; }

        /// <summary>
        /// Raw altimeter value
        /// </summary>
        [DataMember(Name = "rawValue", EmitDefaultValue = false)]
        public string RawValue { get; init; }

        #region Constructors

        /// <summary>
        /// Default
        /// </summary>
        public AltimeterSetting() { }

        internal AltimeterSetting(string[] tokens, List<string> errors)
        {
            if (tokens.Length == 0)
            {
                errors.Add("Array with altimeter token is empty");
                return;
            }

            RawValue = string.Join("", tokens);

            var altimeterToken = tokens.First();

            UnitType = EnumTranslator.GetValueByDescription<AltimeterUnitType>(altimeterToken[..1]);
            Value = int.Parse(altimeterToken[1..]);
        }

        #endregion
    }
}
