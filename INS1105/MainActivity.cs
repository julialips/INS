using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System.Collections.Generic;
using System.Text;
using Context = Android.Content.Context;
using Xamarin.Essentials;
using Android.Hardware;
using System.Runtime.Remoting.Contexts;
using System;
using System.Linq;

namespace INS1105
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ISensorEventListener
    {
        double dt; // отрезое между снятием ускорения в 2 точках
        double allt; //все время 
        long lasttime;
        readonly double[] v = new double[3]; //скорость
        readonly double[] dr = new double[3];  //перемемещение
        
        protected SensorManager msensorManager; //Менеджер сенсоров 
        
        static MadgwickAHRS AHRS = new MadgwickAHRS(1f / 256f, 5f);
        private float[] accelData; // массив ускорений по 3-м осям в формате xyzxyz...
        private float[] giroscopeData;
        //private float[] magnitometrData;

        protected Button start;
        protected Button stop;
        protected Button show;

        private TextView xView;
        private TextView yView;
        private TextView zView;

        private TextView vx;
        private TextView vy;
        private TextView vz;

        private TextView drx;
        private TextView dry;
        private TextView drz;

        private TextView girox;
        private TextView giroy;
        private TextView giroz;

        public TextView QuaterionField;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            msensorManager = (SensorManager)GetSystemService(Context.SensorService);
            accelData = new float[3];
            xView = (TextView)FindViewById(Resource.Id.textViewValueAccelX);
            yView = (TextView)FindViewById(Resource.Id.textViewValueAccelY);
            zView = (TextView)FindViewById(Resource.Id.textViewValueAccelZ);

            vx = (TextView)FindViewById(Resource.Id.textViewValueVelocityX);
            vy = (TextView)FindViewById(Resource.Id.textViewValueVelocityY);  // поля для значений скоростей
            vz = (TextView)FindViewById(Resource.Id.textViewValueVelocityZ);

            drx = (TextView)FindViewById(Resource.Id.textViewValueMigrationX);
            dry = (TextView)FindViewById(Resource.Id.textViewValueMigrationY);  // поля для значений перемещений
            drz = (TextView)FindViewById(Resource.Id.textViewValueMigrationZ);

            QuaterionField = (TextView)FindViewById(Resource.Id.textViewValueQuaternion);

            girox = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeX);
            giroy = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeY);  // поля для значений гироскопа
            giroz = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeZ);

            start = FindViewById<Button>(Resource.Id.buttonSet0); //Инициализация кнопки старт
            stop = FindViewById<Button>(Resource.Id.buttonReset);   //Инициализация кнопки стоп

            start.Click += delegate (object sender, EventArgs e)
            {
                start.Text = "Running...";
            };
            stop.Click += delegate (object sender, EventArgs e)
            {
                // на кнопку Стоп происходит обнуление накопленных значение по скорости и перемещению
                start.Text = "START";
                dr[0] = 0;
                dr[1] = 0;
                dr[2] = 0;
                v[0] = 0;
                v[1] = 0;
                v[2] = 0;
                allt = 0;
            };
        }
        /*public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }*/
        override protected void OnResume()
        {
            base.OnResume();
            msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Game);
            msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(SensorType.Gyroscope), SensorDelay.Game);
            //msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(SensorType.MagneticField), SensorDelay.Game);
            //msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(Sensor.TYPE_MAGNETIC_FIELD), SensorManager.SENSOR_DELAY_UI);     
        }
        override protected void OnPause()
        {
            base.OnPause();
            msensorManager.UnregisterListener(this, msensorManager.GetDefaultSensor(SensorType.Accelerometer));
            msensorManager.UnregisterListener(this, msensorManager.GetDefaultSensor(SensorType.Gyroscope));
        }

        private void LoadNewSensorData(SensorEvent e)
        {
            //Определяем тип датчика
            var type = e.Sensor.Type;
            if (type == SensorType.Gyroscope)
            {
                giroscopeData = e.Values.ToArray();            
            }

            if (type == SensorType.Accelerometer)
            {
                accelData = e.Values.ToArray();
                //Получение времени Integrirovanie(accelData);
                dt = (e.Timestamp - lasttime) * 1e-9;
                //время между двумя последними событиями(снятиями показаний с датчика)
                lasttime = e.Timestamp;
                //все время от нажатия на сброс
                allt += dt;
                //первое интегрирование, получение скорости
                v[0] += accelData[0] * dt;
                v[1] += accelData[1] * dt;
                v[2] += accelData[2] * dt;
                //второе интегрирование, получение перемещения по каждой из координат
                dr[0] += v[0] * dt;
                dr[1] += v[1] * dt;
                dr[2] += v[2] * dt;
            }
        }
        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        { }

        public void OnSensorChanged(SensorEvent e)
        {
            LoadNewSensorData(e);
            xView.Text = (accelData[0]).ToString();
            yView.Text = (accelData[1]).ToString();
            zView.Text = (accelData[2]).ToString();

            vx.Text = (v[0]).ToString();
            vy.Text = (v[1]).ToString();
            vz.Text = (v[2]).ToString();

            drx.Text = (dr[0]).ToString();
            dry.Text = (dr[1]).ToString();
            drz.Text = (dr[2]).ToString();

            if (giroscopeData != null)
            {
                girox.Text = (giroscopeData[0]).ToString();
                giroy.Text = (giroscopeData[1]).ToString();
                giroz.Text = (giroscopeData[2]).ToString();
            }
            if (giroscopeData != null && accelData != null)
            {
                // MadgwickAHRS madgwick = new MadgwickAHRS(1f / 256f,1);
                AHRS.Update(deg2rad(giroscopeData[0]), deg2rad(giroscopeData[1]), deg2rad(giroscopeData[2]), accelData[0], accelData[1], accelData[2]);
                //выводим в текстовое поле значение кватерниона из свойства класса MadgwickAHRS {get;set}
                QuaterionField.Text = (AHRS.Quaternion).ToString();
               // преобразование кватерyионов в углы эйлера
                static float deg2rad(float degrees)
                {
                    return (float)(Math.PI / 180) * degrees;
                }
            }
        }
    }
    
    public class MadgwickAHRS
    {
        // Gets or sets the sample period.
        public float SamplePeriod { get; set; }

        // Gets or sets the algorithm gain beta.
        public float Beta { get; set; }

        /// Gets or sets the Quaternion output.
        // public float[] Quaternion { get; set; } так в оригинате, 07.05
        public float[] Quaternion
        {
            get { return Quaternion; }//так я сделала, по сути просто явно определила, кажется это не обязаельно
            set { }
        }
        /// <summary>
        /// Инициализация нового экземпляра класса <see cref="MadgwickAHRS"/> 
        /// </summary>
        /// <param name="samplePeriod">
        /// Период выборки
        /// </param>
      //  public MadgwickAHRS(float samplePeriod) : this(samplePeriod, 1f)// этот я использую
        //{ }

        /// <summary>
        /// Инициализация нового экземпляра класса <see cref="MadgwickAHRS"/> 
        /// </summary>
        /// <param name="samplePeriod">
        /// Период выборки.
        /// </param>
        /// <param name="beta">
        /// Algorithm gain beta.
        /// </param>
        public MadgwickAHRS(float samplePeriod, float beta)
        {
            SamplePeriod = samplePeriod;
            Beta = beta;
            Quaternion = new float[] { 1f, 0f, 0f, 0f };
        }

        /// Algorithm IMU update method. Requires only gyroscope and accelerometer data.
        /// <param name="gx", <param name="gy",<param name="gz",<param name="ax",<param name="ay",<param name="az",>
        /// Measurement in radians/s.
        /// Optimised for minimal arithmetic. Total ±: 45. Total *: 85. Total /: 3. Total sqrt: 3

        public void Update(float gx, float gy, float gz, float ax, float ay, float az)
        {
            float q1 = Quaternion[0], q2 = Quaternion[1], q3 = Quaternion[2], q4 = Quaternion[3];
            float norm;
            float s1, s2, s3, s4;
            float qDot1, qDot2, qDot3, qDot4;
            // Вспомогательные переменные, чтобы избежать повторной арифметики
            float _2q1 = 2f * q1;
            float _2q2 = 2f * q2;
            float _2q3 = 2f * q3;
            float _2q4 = 2f * q4;
            float _4q1 = 4f * q1;
            float _4q2 = 4f * q2;
            float _4q3 = 4f * q3;
            float _8q2 = 8f * q2;
            float _8q3 = 8f * q3;
            float q1q1 = q1 * q1;
            float q2q2 = q2 * q2;
            float q3q3 = q3 * q3;
            float q4q4 = q4 * q4;
            // Нормализация измерений акселерометра
            norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
            if (norm == 0f) return; // handle NaN
            norm = 1 / norm;        // use reciprocal for division
            ax *= norm;
            ay *= norm;
            az *= norm;
            // Метод градиентного спуска
            s1 = _4q1 * q3q3 + _2q3 * ax + _4q1 * q2q2 - _2q2 * ay;
            s2 = _4q2 * q4q4 - _2q4 * ax + 4f * q1q1 * q2 - _2q1 * ay - _4q2 + _8q2 * q2q2 + _8q2 * q3q3 + _4q2 * az;
            s3 = 4f * q1q1 * q3 + _2q1 * ax + _4q3 * q4q4 - _2q4 * ay - _4q3 + _8q3 * q2q2 + _8q3 * q3q3 + _4q3 * az;
            s4 = 4f * q2q2 * q4 - _2q2 * ax + 4f * q3q3 * q4 - _2q3 * ay;
            norm = 1f / (float)Math.Sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);    // normalise step magnitude
            s1 *= norm;
            s2 *= norm;
            s3 *= norm;
            s4 *= norm;
            // Вычисление скорости изменения кватерниона
            qDot1 = 0.5f * (-q2 * gx - q3 * gy - q4 * gz) - Beta * s1;
            qDot2 = 0.5f * (q1 * gx + q3 * gz - q4 * gy) - Beta * s2;
            qDot3 = 0.5f * (q1 * gy - q2 * gz + q4 * gx) - Beta * s3;
            qDot4 = 0.5f * (q1 * gz + q2 * gy - q3 * gx) - Beta * s4;
            //  Интегрирование для получения кватерниона
            q1 += qDot1 * SamplePeriod;
            q2 += qDot2 * SamplePeriod;
            q3 += qDot3 * SamplePeriod;
            q4 += qDot4 * SamplePeriod;
            //нормализация кватерниона
            norm = 1f / (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);
            Quaternion[0] = q1 * norm;
            Quaternion[1] = q2 * norm;
            Quaternion[2] = q3 * norm;
            Quaternion[3] = q4 * norm;
        }
    }

}

