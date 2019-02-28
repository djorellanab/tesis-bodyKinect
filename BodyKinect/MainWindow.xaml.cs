using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace BodyKinect
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Atributos
        /// <summary>
        /// Activa el sensor Kinect
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Reconocedor de cuerpos del kinect (6 personas)
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// Indice que define a la persona que esta escaneando (Vector body)
        /// </summary>
        private int activeBodyIndex = 0;

        /// <summary>
        /// Lector de frames (Imagenes) del cuerpo
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Define el estado que se encuentra el kinect
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Dibuja el cuerpo en la interfaz de usuario
        /// </summary>
        private KinectBodyView kinectBodyView = null;

        /// Falta los datos de Analizador de gesturas

        /// <summary>
        /// Temporizador de actualizacion de los frames de kinect cada 60 fps
        /// </summary>
        private DispatcherTimer dispatcherTimer = null;

        /// <summary>
        /// Evento que permie que las propiedades sean cambiados por valores nuevos
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Obtiene el estado del Kinect en la interfaz de usuario
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            private set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #region Funcionalidades del componentes
        /// <summary>
        /// Constructor que inicializa los componentes de la interfaz
        /// </summary>
        public MainWindow()
        {
            // Inicializa todos los componentes
            InitializeComponent();

            // Obtiene el sensor del kinect (Solo uno)
            this.kinectSensor = KinectSensor.GetDefault();

            // Abre el sensor
            this.kinectSensor.Open();

            // Actualiza el estado del kinect
            this.UpdateKinectStatusText();

            // Obtiene los frames respectivo del kinect
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // Objeto que renderiza el cuerpo en la interfaz de usuario
            this.kinectBodyView = new KinectBodyView(this.kinectSensor);

            // Analizador de gestores

            // Asigna los datos a la interfaz
            this.DataContext = this;
            this.kinectBodyViewbox.DataContext = this.kinectBodyView;
        }

        #endregion

        #region metodos

        /// <summary>
        /// Actualiza el estado del kinect
        /// </summary>
        private void UpdateKinectStatusText()
        {
            // reset the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;
        }

        /// <summary>
        /// Notifica al interfaz de usuario que esta propieda ha cambiado
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Recupera los datos reciente del kinect 
        /// </summary>
        private void UpdateKinectFrameData()
        {
            // Variable local para verificar si ha recibido un dato
            bool dataReceived = false;

            // Obtiene los ultimos datos (Frames)
            using (var bodyFrame = this.bodyFrameReader.AcquireLatestFrame())
            {
                // Detecta si hay un cuerpo
                if (bodyFrame != null)
                {
                    // Si no existe un cuerpo
                    if (this.bodies == null)
                    {
                        // Crea el arreglo de los cuerpo correspondiente (6 Cuerpos)
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // La primera vez se refresca los datos y le asigna un cuerpo al vector, si solo si no se encuentra nulos los objetos (Reutilizables)
                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                    // Verifica el cuerpo correspondiente
                    if (!this.bodies[this.activeBodyIndex].IsTracked)
                    {
                        // Se ha identificado el cuerpo, por lo cual le asigna el analisis de otro cuerpo
                        int bodyIndex = this.GetActiveBodyIndex();

                        // Verifica que se ha obtenido un cuerpo analizable
                        if (bodyIndex > 0)
                        {
                            // Actualiza el indice
                            this.activeBodyIndex = bodyIndex;
                        }
                    }
                    // Bandera que le indica que se recibio un dato
                    dataReceived = true;
                }
            }

            // Verifica que hay un dato recibido
            if (dataReceived)
            {
                // Obtiene el cuerpo a analizar
                Body activeBody = this.bodies[this.activeBodyIndex];
                // actualiza el nuevo cuerpo
                this.kinectBodyView.UpdateBodyData(activeBody);

                // Analisis de gestura
            }
        }

        #endregion

        #region funciones

        /// <summary>
        /// Obtiene el indice de cuerpo a analizar
        /// </summary>
        /// <returns>el indice de cuerpo ha analizar o -1 si no analiza ningun cuerpo</returns>
        private int GetActiveBodyIndex()
        {
            // Variable local que verifica el indice del cuerpo
            int activeBodyIndex = -1;
            // Obtiene del kinect el numero de cuerpos analizado
            int maxBodies = this.kinectSensor.BodyFrameSource.BodyCount;

            // Recorre todos los cuerpo ha analizar
            for (int i = 0; i < maxBodies; ++i)
            {
                // Verifica que el cuerpo actual esta en seguimiento asi mismo verifica que tenga rastreo de manos
                if (this.bodies[i].IsTracked && (this.bodies[i].HandRightState != HandState.NotTracked || this.bodies[i].HandLeftState != HandState.NotTracked))
                {
                    // Finaliza y actualiza el cuerpo
                    activeBodyIndex = i;
                    break;
                }
            }

            // Retorna el indice a analizar
            return activeBodyIndex;
        }

        #endregion

        #region Eventos

        /// <summary>
        /// Evento al cargar la ventana principal, cuya funcionalidad es inicializar el temporizador
        /// y actualizar los objetos a 60 fps
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, EventArgs e)
        {
            // agrego Evento de renderizacion
            CompositionTarget.Rendering += this.DispatcherTimer_Tick;

            // Seteo de actualizacion 60 fps
            this.dispatcherTimer = new DispatcherTimer();
            // Por cada frame se actualiza la pantalla
            this.dispatcherTimer.Tick += this.DispatcherTimer_Tick;
            // Defino 60 fps
            this.dispatcherTimer.Interval = TimeSpan.FromSeconds(1 / 60);
            // Comienza el reloj
            this.dispatcherTimer.Start();
        }

        /// <summary>
        /// Evento al cerrar o apagar el programa
        /// </summary>
        /// <param name="sender">Objeto de la ventana</param>
        /// <param name="e">Argumentos del evento</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Elimino el evento de renderizacion
            CompositionTarget.Rendering -= this.DispatcherTimer_Tick;

            // Verifico si el reloj esta activo
            if (this.dispatcherTimer != null)
            {
                // Apago el reloj
                this.dispatcherTimer.Stop();
                // Elimino el evento por cada accion del reloj (tick)
                this.dispatcherTimer.Tick -= this.DispatcherTimer_Tick;
            }

            // Veriifico si el lector del cuerpo esta activo
            if (this.bodyFrameReader != null)
            {
                // Elimino todos los datos del lector del cuerpo
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }
            /*
            if (this.gestureDetector != null)
            {
                // The GestureDetector contains disposable members (VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader)
                this.gestureDetector.Dispose();
                this.gestureDetector = null;
            }*/

            // Verifico que el kinect esta utlizando
            if (this.kinectSensor != null)
            {
                // Apago el kinect
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Renderizacion de frames
        /// </summary>
        /// <param name="sender">objeto enviado por el evento</param>
        /// <param name="e">Argumentos del evento</param>
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Actualiza el estado del kinect
            this.UpdateKinectStatusText();
            this.UpdateKinectFrameData();
        }

        #endregion
    }
}
