//------------------------------------------------------------------------------
// <copyright file="GestureResultView.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace BodyKinect
{
    using BodyKinect.Common;
    using System;

    /// <summary>
    /// Objeto que almacena los valores correspondientes y asi mismo notifica su acgtualizacion
    /// </summary>
    public sealed class GestureResultView : BindableBase
    {
        #region  Atributos
        /// <summary>
        /// Valor continuo que verifica el progreso de levantamiento de mano
        /// </summary>
        private float handsUpProgress = 0.0f;

        /// <summary> 
        /// Valor de UI
        /// </summary>
        public float HandsUpProgress
        {
            get
            {
                return this.handsUpProgress;
            }

            private set
            {
                this.SetProperty(ref this.handsUpProgress, value);
            }
        }


        /// <summary>
        /// Verifica si el cuerpo actual tiene seguimiento
        /// </summary>
        private bool isTracked = false;

        /// <summary>
        /// Valor que indica si el cuerpo  asociado al detector tiene seguimiento
        /// </summary>
        public bool IsTracked
        {
            get
            {
                return this.isTracked;
            }

            private set
            {
                this.SetProperty(ref this.isTracked, value);
            }
        }

        #endregion

        #region constructor
        /// <summary>
        /// Constructor que inicializa todos los componentes
        /// </summary>
        /// <param name="isTracked">Variable de seguimiento</param>
        /// <param name="progress">Valor de proceso</param>
        public GestureResultView(bool isTracked, float progress)
        {
            this.HandsUpProgress = progress;
            this.IsTracked = isTracked;
        }


        /// <summary>
        /// Actualiza los valores desplegado en la interfaza de usuario
        /// </summary>
        /// <param name="isBodyTrackingIdValid">Verifica que el cuerpo tenga seguimiento</param>
        /// <param name="progress">El valor del progreso continuo</param>
        public void UpdateGestureResult(bool isBodyTrackingIdValid, float progress)
        {
            // Actualiza seguimiento
            this.IsTracked = isBodyTrackingIdValid;

            // Si no hay seguimiento, se asigna valores por default
            if (!this.isTracked)
            {
                this.HandsUpProgress = -1.0f;
            }
            else // Si hay, Se asigna los valores pasado
            {
                this.HandsUpProgress = progress;
            }
        }
        #endregion
    }
}
