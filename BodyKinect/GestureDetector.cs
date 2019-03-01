//------------------------------------------------------------------------------
// <copyright file="GestureDetector.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace BodyKinect
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using Microsoft.Kinect.VisualGestureBuilder;

    /// <summary>
    /// Objeto que obtiene los resultados obtenidos de Visual Gesture Builder
    /// </summary>
    public sealed class GestureDetector : IDisposable
    {
        #region atributos
        /// <summary>
        /// Path de la base de datos de entrenamiento
        /// </summary>
        private readonly string gestureDatabase = @"Database\DEMOYT3.gbd";

        /// <summary>
        /// Nombre de la variable continua, que verifica el levantamiento de brazos
        /// </summary>
        private readonly string hu3 = "HU3";

        /// <summary>
        /// obtiene los frames de gesturas
        /// </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary>
        ///  Obtiene el identificador del cuerpo que se encuentra detectando actualmente
        /// </summary>
        public ulong TrackingId
        {
            get
            {
                return this.vgbFrameSource.TrackingId;
            }

            set
            {
                if (this.vgbFrameSource.TrackingId != value)
                {
                    this.vgbFrameSource.TrackingId = value;
                }
            }
        }

        /// <summary>
        ///  Verifica si la deteccion se encuentra pausado
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return this.vgbFrameReader.IsPaused;
            }

            set
            {
                if (this.vgbFrameReader.IsPaused != value)
                {
                    this.vgbFrameReader.IsPaused = value;
                }
            }
        }

        /// <summary>
        /// Obtiene la lecturas de gesturas
        /// </summary>
        private VisualGestureBuilderFrameReader vgbFrameReader = null;

        /// <summary>
        /// Almacena los resultados respectivos de la gestura para mostrarlo en la UI
        /// </summary>
        public GestureResultView GestureResultView { get; private set; }


        #endregion

        #region constructor
        /// <summary>
        /// Constructor que instancias los atributos correspondientes
        /// </summary>
        /// <param name="kinectSensor"></param>
        /// <param name="gestureResultView"></param>
        public GestureDetector(KinectSensor kinectSensor, GestureResultView gestureResultView)
        {
            // Verifica si el kinect esta conectado
            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            // Verifica si existe el objeto que almacenara los resultados
            if (gestureResultView == null)
            {
                throw new ArgumentNullException("gestureResultView");
            }

            // Le asigna los valores correspondiente a los atributos globales
            this.GestureResultView = gestureResultView;

            // Crea el objeto para asociar los recursos, respecto al cuerpo que se esta analizando
            this.vgbFrameSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);

            // Obtiene los valores de lecturas (En caso que no se tenga la lectura se pausa)
            this.vgbFrameReader = this.vgbFrameSource.OpenReader();
            if (this.vgbFrameReader != null)
            {
                this.vgbFrameReader.IsPaused = true;
            }
            // Obtiene toda la informacion de la base de datos
            using (var database = new VisualGestureBuilderDatabase(this.gestureDatabase))
            {
                this.vgbFrameSource.AddGestures(database.AvailableGestures);
            }

        }
        #endregion


        #region metodos

        /// <summary>
        /// Metodo para limpiar las lecturas correspondientes
        /// </summary>
        public void Dispose()
        {
            if (this.vgbFrameReader != null)
            {
                this.vgbFrameReader.Dispose();
                this.vgbFrameReader = null;
            }

            if (this.vgbFrameSource != null)
            {
                this.vgbFrameSource.Dispose();
                this.vgbFrameSource = null;
            }
        }

        /// <summary>
        /// Actualiza la ultima lectura del sensor de gesturas
        /// </summary>
        public void UpdateGestureData()
        {
            // Obtiene el respectivo frame a analizar
            using (var frame = this.vgbFrameReader.CalculateAndAcquireLatestFrame())
            { 
                // Verifica si el frame existe
                if (frame != null)
                {
                    // Obtiene los resultados del frame de valores coninuo
                    var continuousResults = frame.ContinuousGestureResults;
                    // Obtiene los resulados del proceso
                    float handsUpsProgress = this.GestureResultView.HandsUpProgress;
                    // Recorre todas las gestura analizar
                    foreach (var gesture in this.vgbFrameSource.Gestures)
                    {
                        // Verifica que tenga el paquete resultado tenga valiables continuas 
                        if (continuousResults != null)
                        {
                            // Verifica los atributos a analizar
                            if (gesture.Name.Equals(this.hu3) && gesture.GestureType == GestureType.Continuous)
                            {
                                // Obtengo los valores continuos
                                ContinuousGestureResult result = null;
                                continuousResults.TryGetValue(gesture, out result);
                                // Verifica si tienes resultados
                                if (result != null)
                                {
                                    handsUpsProgress = result.Progress;
                                }
                            }
                        }
                        // Obtiene resultado dentro del parametro continuo
                        if (handsUpsProgress < 0)
                        {
                            handsUpsProgress = 0;
                        }
                        else if (handsUpsProgress > 3)
                        {
                            handsUpsProgress = 3;
                        }
                        // Actualiza los resultados
                        this.GestureResultView.UpdateGestureResult(true, handsUpsProgress);
                    }
                }
            }
        }
        #endregion
    }
}
