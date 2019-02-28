using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Kinect;

namespace BodyKinect
{
    /// <summary>
    /// Visualizaciòn del cuerpo del Kinect para mostrarlo en la interfaz de usuario
    /// </summary>
    public sealed class KinectBodyView
    {
        #region constantes
        /// <summary>
        /// El circulo de radio que dibuja las manos
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// El espesor de la linea de conexión 
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// El espesor de los huesos
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constante para manejar los valores negativos de Z (Profundidad)
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        #endregion

        #region Identificacion de colores
        /// <summary>
        /// Color de pincel (Verde claro), para identificar las manos cerrada
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Color de pincel (Morado), para identificar las manos abiertas
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Color de pincel (rojo) que sigue el puntero de las manos 
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Color de pincel (Rosado) que sige el rastro de las uniones
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Color de pincel amarillo para inferir las ariculaciones (Supone)
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary> 
        /// color gris, que infiere los huesos (Supone)
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary> 
        /// Color rojo, que no infiere los huesos
        /// </summary>
        private readonly Pen bonePen = new Pen(Brushes.Red, 6);
        #endregion

        #region renderizacion
        /// <summary>
        /// Renderizacion del cuerpo humano
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary> 
        /// Renderizacion de una imagen
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Mapeo de un punto a otro
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Definicion de los huesos humanos
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Ancho de la imagen
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Alto de la imagen
        /// </summary>
        private int displayHeight;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor de la clase que inicializa todos los componentes
        /// </summary>
        /// <param name="kinectSensor">Instancia del sensor</param>
        public KinectBodyView(KinectSensor kinectSensor)
        {
            // Validacion si el sensor esta conectado
            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            // obtiene el mapeo del kinect
            this.coordinateMapper = kinectSensor.CoordinateMapper;

            // Obtiene la profundidad del kinect
            FrameDescription frameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            // Obtiene el ancho y la altura que analiza el kinect
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // Instancia el conjunto de Huesos
            this.bones = new List<Tuple<JointType, JointType>>();

            // Dibujo del torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Dibujo del brazo derecho
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Dibujo del brazo izquierdo
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Dibujo de pierna derecha
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Dibujo de la pierna izquierda
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // Instancia de todo el cuerpo 
            this.drawingGroup = new DrawingGroup();

            // Control de imagen
            this.imageSource = new DrawingImage(this.drawingGroup);
        }

        #endregion

        #region Funciones

        /// <summary>
        /// Obtiene el mapeo de bits que muestra el esqueleto
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        #endregion

        #region metodos

        #region metodos para pintado

        /// Muestra los bordes del cuadro
        /// </summary>
        /// <param name="body">Cuerpo</param>
        /// <param name="drawingContext">Pintador</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            // Obtiene el valor del bordado (Bandera)
            FrameEdges clippedEdges = body.ClippedEdges;

            // Verifica el borde abajo 
            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            // Verifica el borde superior
            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            // verifica el borde izquierdo
            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            // verificar el borde derecho
            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Dibuja solo un hueso
        /// </summary>
        /// <param name="joints">El conjunto de uniones</param>
        /// <param name="jointPoints">Transformacion de posicion de cada union</param>
        /// <param name="jointType0">Primera union</param>
        /// <param name="jointType1">Segundo union</param>
        /// <param name="drawingContext">Herramienta para dibujar</param>
        /// <param name="drawingPen">Color del lapiz</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            // Obtiene las uniones
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // Si no tiene seguimiento ambas uniones, no lo dibuja
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // Une ambas uniones, Si solo si se tiene seguimiento
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }
            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Metodo que dibuja el cuerpo humano
        /// </summary>
        /// <param name="joints">Uniones para dibujar</param>
        /// <param name="jointPoints">Transformacion del punto de union</param>
        /// <param name="drawingContext">Herramienta de dibujo</param>
        /// <param name="drawingPen">Color a especificar del lapiz</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Dibuja los huesos
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Metodo que dibuja elipse de mano
        /// </summary>
        /// <param name="handState">Estado de la mano</param>
        /// <param name="handPosition">Posicion de la mano (der-izq)</param>
        /// <param name="drawingContext">Herramienta de dibujo</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        #endregion
        /// <summary>
        /// Actualiza la imagen del cuerpo con la nueva informacion
        /// Se debe llamar cuando se llama el metodo AcquireLatestFrame
        /// </summary>
        /// <param name="body">Cuerpo desde la interfaz de usuario</param>
        public void UpdateBodyData(Body body)
        {
            // Verifica si existe el cuerpo
            if (body != null)
            {
                // Dibuja el cuerpo
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Dibuja un rectangulo negro con la altura y ancho respectivo
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                    // Verifica si identifica el cuerpo
                    if (body.IsTracked)
                    {
                        this.DrawClippedEdges(body, dc);

                        // Obtiene las uniones respectivas (Solo lectura)
                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // Obtiene el diccionario de datos de uniones
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                        // Recorre todas las uniones
                        foreach (JointType jointType in joints.Keys)
                        {
                            // Aveces obtiene el valor de profundidad (Z) negativo
                            // Y se reduce a 0.01f para evitar un valor infinito
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            // Representacion del punto en 2 dimensiones
                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            // Nuevo Punto
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                        }

                        this.DrawBody(joints, jointPoints, dc, this.bonePen);

                        this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                        this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                    }
                }
            }
        }
        #endregion
    }
}
