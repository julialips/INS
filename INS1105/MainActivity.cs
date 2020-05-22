﻿using Android.App;
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
using Android.Text;
using Android.Util;

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
        //static MadgwickAHRS AHRS = new MadgwickAHRS(1f / 256f, 5f);
        private float[] accelData; // массив ускорений по 3-м осям в формате xyzxyz...
        private float[] accelDataClbr; // массив ускорений по 3-м осям в формате xyzxyz...
        private float[] giroscopeData;
        //private float[] magnitometrData;

        protected Button start;
        protected Button stop;
        protected Button reset;
        protected Button calibrate;

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

        public TextView QuaterionFieldX;
        public TextView QuaterionFieldY;
        public TextView QuaterionFieldZ;
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

            QuaterionFieldX = (TextView)FindViewById(Resource.Id.textViewValueQuaternionX);
            QuaterionFieldY = (TextView)FindViewById(Resource.Id.textViewValueQuaternionY);
            QuaterionFieldZ = (TextView)FindViewById(Resource.Id.textViewValueQuaternionZ);
            QuaterionField = (TextView)FindViewById(Resource.Id.textViewValueQuaternion);

            girox = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeX);
            giroy = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeY);  // поля для значений гироскопа
            giroz = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeZ);

            start = FindViewById<Button>(Resource.Id.buttonSet0);
            stop = FindViewById<Button>(Resource.Id.buttonStop);   
            reset = FindViewById<Button>(Resource.Id.buttonReset);
            calibrate = FindViewById<Button>(Resource.Id.buttonCalibrate);

            OnPause();
            start.Click += delegate (object sender, EventArgs e)
            {
                start.Text = "Running...";
                OnResume();
            };
            stop.Click += delegate (object sender, EventArgs e)
            {
                start.Text = "START";
                OnPause();

            };
            reset.Click += delegate (object sender, EventArgs e)
            {
                // на кнопку recet происходит обнуление накопленных значение по скорости и перемещению
                dr[0] = 0;
                dr[1] = 0; 
                dr[2] = 0;
                v[0] = 0;  
                v[1] = 0;  
                v[2] = 0;
                allt = 0;

                vx.Text = (v[0]).ToString("0.000" + "m/s");
                vy.Text = (v[1]).ToString("0.000" + "m/s");
                vz.Text = (v[2]).ToString("0.000" + "m/s");

                drx.Text = (dr[0]).ToString("0.000" + "m");
                dry.Text = (dr[1]).ToString("0.000" + "m");
                drz.Text = (dr[2]).ToString("0.000" + "m");
            };
            calibrate.Click += delegate (object sender, EventArgs e)
            {
               summx = 0, summy = 0, summz = 0;
                calibratex = 0;
                calibratey = 0;
                calibratez = 0;
                counter = 0;
            };

        }

        public float CalibrateMetod(float []acceldata)
        {
            float calibratex = 0;
            float calibratey = 0;
            float calibratez = 0;
            int count = 0;
            for (int i = 0; i <= 10; i++)
            {
                count++;
                float summx = 0,
                      summy = 0,
                      summz = 0;
                summx += accelData[0];
                summy += accelData[1];// сюда получается будут все значения с датчика, не 10, как хотелось бы
                summz += accelData[2];

                if (i == 10)
                {
                    calibratex = summx / 10;
                    calibratey = summx / 10;
                    calibratez = summx / 10;
                }
            }
            return calibratex;
            // accelData[1]
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

                float summx = 0, summy = 0, summz = 0;
                float calibratex = 0;
                float calibratey = 0;
                float calibratez = 0;
                int counter = 0;

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
                accelData = e.Values.ToArray();            //Получение времени Integrirovanie(accelData);
                dt = (e.Timestamp - lasttime) * 1e-9;
                lasttime = e.Timestamp;                  //время между двумя последними событиями(снятиями показаний с датчика)
                allt += dt;//все время от нажатия на сброс
                                
                summx += accelData[0];
                summy += accelData[1];
                summz += accelData[2];    
                
                counter++;

                if (counter == 10)
                {
                    calibratex = summx / 10;
                    calibratey = summy / 10;
                    calibratez = summz / 10;
                 }
                    
                    accelDataClbr[0] = accelData[0] - calibratex;
                    accelDataClbr[1] = accelData[1] - calibratey;
                    accelDataClbr[2] = accelData[2] - calibratez;

                    v[0] += (accelDataClbr[0]) * dt;
                    v[1] += (accelDataClbr[1]) * dt;   //первое интегрирование, получение скорости
                    v[2] += (accelDataClbr[2]) * dt;

                    dr[0] += v[0] * dt;
                    dr[1] += v[1] * dt;       //второе интегрирование, получение перемещения по каждой из координат
                    dr[2] += v[2] * dt;                        
            }
        }
        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        { }

        public void OnSensorChanged(SensorEvent e)
        {
           LoadNewSensorData(e);
            
            xView.Text = (accelData[0]- calibratex).ToString("0.000" + "m/s\u00B2");
            yView.Text = (accelData[1]- calibratey).ToString("0.000" + "m/s\u00B2");
            zView.Text = (accelData[2]- calibratez).ToString("0.000" + "m/s\u00B2");

            vx.Text = (v[0]).ToString("0.000" + "m/s");
            vy.Text = (v[1]).ToString("0.000" + "m/s");
            vz.Text = (v[2]).ToString("0.000" + "m/s");

            drx.Text = (dr[0]).ToString("0.000" + "m");
            dry.Text = (dr[1]).ToString("0.000" + "m");
            drz.Text = (dr[2]).ToString("0.000" + "m");

            if (giroscopeData != null)
            {
                girox.Text = (giroscopeData[0]).ToString("0.000");
                giroy.Text = (giroscopeData[1]).ToString("0.000");
                giroz.Text = (giroscopeData[2]).ToString("0.00000");
            }

            if (giroscopeData != null && accelData != null)
            {
                // MadgwickAHRS madgwick = new MadgwickAHRS(1f / 256f,1);
                AHRS.Update(deg2rad(giroscopeData[0]), deg2rad(giroscopeData[1]), deg2rad(giroscopeData[2]), accelData[0], accelData[1], accelData[2]);
                //выводим в текстовое поле значение кватерниона из свойства класса MadgwickAHRS {get;set}
                QuaterionFieldX.Text = (AHRS.Quaternion[0]).ToString("0.000");
                QuaterionFieldY.Text = (AHRS.Quaternion[1]).ToString("0.000");
                QuaterionFieldZ.Text = (AHRS.Quaternion[2]).ToString("0.000");
                QuaterionField.Text = (AHRS.Quaternion[3]).ToString("0.000");
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
            // get { return Quaternion; }//так я сделала, по сути просто явно определила, кажется это не обязаельно
            get;
            set;
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

