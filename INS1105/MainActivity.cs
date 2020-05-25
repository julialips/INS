using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Context = Android.Content.Context;
using Android.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Numerics;
using Android.Content;

namespace INS1105
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@drawable/iconka4")]
    public class MainActivity : AppCompatActivity, ISensorEventListener
    {
        //public string writePath = @"C:\SomeDir\hta.txt";
        //string text = "Привет мир!\nПока мир...";
        double dt; // отрезое между снятием ускорения в 2 точках
        double allt; //все время 
        long lasttime;
        readonly double[] v = new double[3];
        readonly double[] dr = new double[3];

        protected SensorManager msensorManager;

        static MadgwickAHRS AHRS = new MadgwickAHRS(1f / 256f, 5f);

        private double[] accelData;
        private double[] accelDataCalibrate;
        private double[] giroscopeData;
        private double pitch, tilt, azimuth;
        //  private double[] accelDataClbr;

        protected Button start;
        protected Button stop;
        protected Button reset;
        protected Button calibrate;
        protected Button write;

        //   protected ImageView image;

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

        public TextView Pitch;
        public TextView Tilt;
        public TextView Azimuth;

        public TextView PitchMadj;
        public TextView TiltMadj;
        public TextView AzimuthMadj;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            msensorManager = (SensorManager)GetSystemService(Context.SensorService);
            accelData = new double[3];

            xView = (TextView)FindViewById(Resource.Id.textViewValueAccelX);
            yView = (TextView)FindViewById(Resource.Id.textViewValueAccelY);
            zView = (TextView)FindViewById(Resource.Id.textViewValueAccelZ);

            vx = (TextView)FindViewById(Resource.Id.textViewValueVelocityX);
            vy = (TextView)FindViewById(Resource.Id.textViewValueVelocityY);
            vz = (TextView)FindViewById(Resource.Id.textViewValueVelocityZ);

            drx = (TextView)FindViewById(Resource.Id.textViewValueMigrationX);
            dry = (TextView)FindViewById(Resource.Id.textViewValueMigrationY);
            drz = (TextView)FindViewById(Resource.Id.textViewValueMigrationZ);

            QuaterionFieldX = (TextView)FindViewById(Resource.Id.textViewValueQuaternionX);
            QuaterionFieldY = (TextView)FindViewById(Resource.Id.textViewValueQuaternionY);
            QuaterionFieldZ = (TextView)FindViewById(Resource.Id.textViewValueQuaternionZ);
            QuaterionField = (TextView)FindViewById(Resource.Id.textViewValueQuaternion);

            Pitch = (TextView)FindViewById(Resource.Id.textViewPitch);
            Tilt = (TextView)FindViewById(Resource.Id.textViewTilt);
            Azimuth = (TextView)FindViewById(Resource.Id.textViewAzimuth);

            PitchMadj = (TextView)FindViewById(Resource.Id.textViewPitchMadj);
            TiltMadj = (TextView)FindViewById(Resource.Id.textViewTiltMadj);
            AzimuthMadj = (TextView)FindViewById(Resource.Id.textViewAzimuthMadj);

            girox = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeX);
            giroy = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeY);
            giroz = (TextView)FindViewById(Resource.Id.textViewValueGiroscopeZ);

            start = FindViewById<Button>(Resource.Id.buttonSet0);
            stop = FindViewById<Button>(Resource.Id.buttonStop);
            reset = FindViewById<Button>(Resource.Id.buttonReset);
            calibrate = FindViewById<Button>(Resource.Id.buttonCalibrate);
            write = FindViewById<Button>(Resource.Id.buttonWrite);

            /*
                < ImageView
                android: id = "id/imageVieww"
                android: layout_width = "wrap_content"
                android: layout_height = "400dp"
                android: src = "@drawable/imageproxy"
                android: layout_marginTop = "0.0dp"
             />
                 image = FindViewById<ImageView>(Resource.Id.imageVieww);
                image.SetImageResource(Resource.Drawable.imageproxy);
            */
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
                summx = 0;
                summy = 0;
                summz = 0;
                calibratex = 0;
                calibratey = 0;
                calibratez = 0;
                counter = 0;
            };
            write.Click += delegate (object sender, EventArgs e)
            {
                write.Text = "Writing...";
                WriteFile();
            };
        }
        /* public double CalibrateMetod(double []acceldata)
         {
             double calibratex = 0;
             double calibratey = 0;
             double calibratez = 0;
             int count = 0;
             for (int i = 0; i <= 10; i++)
             {
                 count++;
                 double summx = 0,
                       summy = 0,
                       summz = 0;
                 summx += accelData[0];
                 summy += accelData[1];
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
         }*/

        override protected void OnResume()
        {
            base.OnResume();
            msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(SensorType.LinearAcceleration), SensorDelay.Game);
            msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Game);
            msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(SensorType.Gyroscope), SensorDelay.Game);
            msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(SensorType.RotationVector), SensorDelay.Game);
        }
        override protected void OnPause()
        {
            base.OnPause();
            msensorManager.UnregisterListener(this, msensorManager.GetDefaultSensor(SensorType.LinearAcceleration));
            msensorManager.RegisterListener(this, msensorManager.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Game);
            msensorManager.UnregisterListener(this, msensorManager.GetDefaultSensor(SensorType.Gyroscope));
            msensorManager.UnregisterListener(this, msensorManager.GetDefaultSensor(SensorType.RotationVector));

        }

        double summx = 0, summy = 0, summz = 0;
        double calibratex = 0;
        double calibratey = 0;
        double calibratez = 0;
        int counter = 0;
        public double[] g = null;
        private void LoadNewSensorData(SensorEvent e)
        {
            var type = e.Sensor.Type; //Определяем тип датчика
            if (type == SensorType.Gyroscope)
            {
                giroscopeData = ToArray(e.Values);
            }

            if (type == SensorType.RotationVector)
            {
                double[] g = ToArray(e.Values);

                double norm = Math.Sqrt(g[0] * g[0] + g[1] * g[1] + g[2] * g[2] + g[3] * g[3]);
                g[0] /= norm;
                g[1] /= norm;
                g[2] /= norm;
                g[3] /= norm;
                //Set values to commonly known quaternion letter representatives
                double x = g[0];
                double y = g[1];
                double z = g[2];
                double w = g[3];
                //Calculate Pitch in degrees (-180 to 180)
                double sinP = 2.0 * (w * x + y * z);
                double cosP = 1.0 - 2.0 * (x * x + y * y);

                pitch = Math.Atan2(sinP, cosP) * (180 / Math.PI);

                //Calculate Tilt in degrees (-90 to 90)
                double sinT = 2.0 * (w * y - z * x);
                if (Math.Abs(sinT) >= 1)
                {
                    tilt = Math.PI / 2 * (180 / Math.PI);  // tilt = Math.Copysign(Math.PI / 2, sinT) * (180 / Math.PI); этот вариант правильный, так было в оригинале
                }
                else
                    tilt = Math.Asin(sinT) * (180 / Math.PI);

                //Calculate Azimuth in degrees (0 to 360; 0 = North, 90 = East, 180 = South, 270 = West)
                double sinA = 2.0 * (w * z + x * y);
                double cosA = 1.0 - 2.0 * (y * y + z * z);
                azimuth = Math.Atan2(sinA, cosA) * (180 / Math.PI);
            }
            if (type == SensorType.Accelerometer)
            {
                accelData = ToArray(e.Values);
            }
            if (type == SensorType.LinearAcceleration)
            {
                accelDataCalibrate = ToArray(e.Values);         //Получение времени Integrirovanie(accelData);
                dt = (e.Timestamp - lasttime) * 1e-9;
                lasttime = e.Timestamp;                 //время между двумя последними событиями(снятиями показаний с датчика)
                allt += dt;                             //все время от нажатия на сброс

                summx += accelDataCalibrate[0];
                summy += accelDataCalibrate[1];
                summz += accelDataCalibrate[2];

                counter++;

                if (counter == 50)
                {
                    calibratex = summx / 50;
                    calibratey = summy / 50;
                    calibratez = summz / 50;
                }
                v[0] += (accelDataCalibrate[0] - calibratex) * dt;
                v[1] += (accelDataCalibrate[1] - calibratey) * dt;   //первое интегрирование, получение скорости
                v[2] += (accelDataCalibrate[2] - calibratez) * dt;

                dr[0] += v[0] * dt;
                dr[1] += v[1] * dt;       //второе интегрирование, получение перемещения по каждой из координат
                dr[2] += v[2] * dt;
            }
        }

        double[] ToArray(IEnumerable<float> values)
        {
            return values.Select(val => (double)val).ToArray();
        }
        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        { }
        public void WriteFile()
        {
            String FILENAME = "YULIA";
            /* using (var ios = OpenFileInput(FILENAME))
             { 
             // отрываем поток для записи
             BufferedWriter bw = new BufferedWriter(new OutputStreamWriter(OpenFileRequest(FILENAME)));
             // пишем данные
             bw.Write("dd");
             // закрываем поток
             bw.Close();
             // Log.d(LOG_TAG, "Файл записан");
              }*/

            // string FILENAME = "hello_file";
            //string str = "hello world!";

            /*using (var fos = OpenFileOutput(FILENAME, FileCreationMode.Private))
            {
                //get the byte array
                byte[] bytes = GetBytes(str);
                fos.Write(bytes, 0, bytes.Length);
            }*/
            using (var ios = OpenFileOutput(FILENAME, FileCreationMode.MultiProcess))
            {
                // string strs;
                //  using (OutputStreamWriter sr = new OutputStreamWriter(ios))
                // {
                //   using (BufferedWriter br = new BufferedWriter(sr))
                //  {
                //  StringBuilder sb = new StringBuilder();
                string line = "Yulia";
                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(line);
                // ios.Write(line,0,3);
                ios.Write(bytes, 0, bytes.Length);
                ios.Close();

                // }
                // }

            }

        }
        public void OnSensorChanged(SensorEvent e)
        {
            LoadNewSensorData(e);
            if (accelDataCalibrate != null)
            {
                xView.Text = (accelDataCalibrate[0] - calibratex).ToString("0.000" + "m/s\u00B2");
                yView.Text = (accelDataCalibrate[1] - calibratey).ToString("0.000" + "m/s\u00B2");
                zView.Text = (accelDataCalibrate[2] - calibratez).ToString("0.000" + "m/s\u00B2");

                vx.Text = (v[0]).ToString("0.000" + "m/s");
                vy.Text = (v[1]).ToString("0.000" + "m/s");
                vz.Text = (v[2]).ToString("0.000" + "m/s");

                drx.Text = (dr[0]).ToString("0.000" + "m");
                dry.Text = (dr[1]).ToString("0.000" + "m");
                drz.Text = (dr[2]).ToString("0.000" + "m");
            }
            Pitch.Text = pitch.ToString("0.00" + "°");
            Tilt.Text = tilt.ToString("0.00" + "°");
            Azimuth.Text = azimuth.ToString("0.00" + "°");

            if (giroscopeData != null)
            {
                girox.Text = (giroscopeData[0]).ToString("0.000");
                giroy.Text = (giroscopeData[1]).ToString("0.000");
                giroz.Text = (giroscopeData[2]).ToString("0.000");
            }

            if (giroscopeData != null && accelData != null)
            {
                AHRS.Update(deg2rad(giroscopeData[0]), deg2rad(giroscopeData[1]), deg2rad(giroscopeData[2]), accelData[0], accelData[1], accelData[2]);

                QuaterionFieldX.Text = (AHRS.Quaternion[0]).ToString("0.000");
                QuaterionFieldY.Text = (AHRS.Quaternion[1]).ToString("0.000");
                QuaterionFieldZ.Text = (AHRS.Quaternion[2]).ToString("0.000");
                QuaterionField.Text = (AHRS.Quaternion[3]).ToString("0.000");

                if (PitchMadj != null && TiltMadj != null && AzimuthMadj != null)
                {
                    PitchMadj.Text = (AHRS.Angles[2]).ToString("0.00" + "°");
                    TiltMadj.Text = (AHRS.Angles[1]).ToString("0.00" + "°");
                    AzimuthMadj.Text = (AHRS.Angles[0]).ToString("0.00" + "°");
                }

                static double deg2rad(double degrees)
                {
                    return (double)(Math.PI / 180) * degrees;
                }
            }
        }
    }
    public class MadgwickAHRS
    {
        // Gets or sets the sample period.
        public double SamplePeriod { get; set; }

        // Gets or sets the algorithm gain beta.
        public double Beta { get; set; }

        /// Gets or sets the Quaternion output.
        // public double[] Quaternion { get; set; } так в оригинате, 07.05
        public double[] Quaternion
        {
            get;
            set;
        }
        public double[] Angles
        {
            get;
            set;
        }

        /// <summary>
        /// Инициализация нового экземпляра класса <see cref="MadgwickAHRS"/> 
        /// </summary>
        /// <param name="samplePeriod">
        /// Период выборки.
        /// </param>
        /// <param name="beta">
        /// Algorithm gain beta.
        /// </param>
        public MadgwickAHRS(double samplePeriod, double beta)
        {
            SamplePeriod = samplePeriod;
            Beta = beta;
            Quaternion = new double[] { 1.0, 0.0, 0.0, 0.0 };
            Angles = new double[3];
        }

        /* void writeFileSD()
         {
             // проверяем доступность SD
             if (Environment.GetExternalStorageState().equals(
                 Environment.MediaUnmounted))
             {
                 Log.Debug(LOG_TAG, "SD-карта не доступна: " + Environment.GetExternalStorageState());
                 return;
             }
             // получаем путь к SD
             File sdPath = Environment.GetExternalStoragePublicDirectory(FILENAME_SD);
             // добавляем свой каталог к пути
             sdPath = new File(sdPath.getAbsolutePath() + "/" + DIR_SD);
             // создаем каталог
             sdPath.mkdirs();
             // формируем объект File, который содержит путь к файлу
             File sdFile = new File(sdPath, FILENAME_SD);
             try
             {
                 // открываем поток для записи
                 BufferedWriter bw = new BufferedWriter(new FileWriter(sdFile));
                 // пишем данные
                 bw.Write("Содержимое файла на SD");
                 // закрываем поток
                 bw.Close();
                 //Log.d(LOG_TAG, "Файл записан на SD: " + sdFile.getAbsolutePath());
             }
             catch (IOException e)
             {
                 e.GetBaseException();
             }
         }*/
        /// Algorithm IMU update method. Requires only gyroscope and accelerometer data.
        /// <param name="gx", <param name="gy",<param name="gz",<param name="ax",<param name="ay",<param name="az",>
        /// Measurement in radians/s.
        /// Optimised for minimal arithmetic. Total ±: 45. Total *: 85. Total /: 3. Total sqrt: 3

        public void Update(double gx, double gy, double gz, double ax, double ay, double az)
        {
            double q1 = Quaternion[0], q2 = Quaternion[1], q3 = Quaternion[2], q4 = Quaternion[3];
            double norm;
            double s1, s2, s3, s4;
            double qDot1, qDot2, qDot3, qDot4;

            // Вспомогательные переменные, чтобы избежать повторной арифметики
            double _2q1 = 2f * q1;
            double _2q2 = 2f * q2;
            double _2q3 = 2f * q3;
            double _2q4 = 2f * q4;
            double _4q1 = 4f * q1;
            double _4q2 = 4f * q2;
            double _4q3 = 4f * q3;
            double _8q2 = 8f * q2;
            double _8q3 = 8f * q3;
            double q1q1 = q1 * q1;
            double q2q2 = q2 * q2;
            double q3q3 = q3 * q3;
            double q4q4 = q4 * q4;

            // Нормализация измерений акселерометра
            norm = (double)Math.Sqrt(ax * ax + ay * ay + az * az);
            if (norm == 0f) return;
            norm = 1.0 / norm;
            ax *= norm;
            ay *= norm;
            az *= norm;

            // Метод градиентного спуска
            s1 = _4q1 * q3q3 + _2q3 * ax + _4q1 * q2q2 - _2q2 * ay;
            s2 = _4q2 * q4q4 - _2q4 * ax + 4f * q1q1 * q2 - _2q1 * ay - _4q2 + _8q2 * q2q2 + _8q2 * q3q3 + _4q2 * az;
            s3 = 4f * q1q1 * q3 + _2q1 * ax + _4q3 * q4q4 - _2q4 * ay - _4q3 + _8q3 * q2q2 + _8q3 * q3q3 + _4q3 * az;
            s4 = 4f * q2q2 * q4 - _2q2 * ax + 4f * q3q3 * q4 - _2q3 * ay;
            norm = 1f / (double)Math.Sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);

            s1 *= norm;
            s2 *= norm;
            s3 *= norm;
            s4 *= norm;

            // Вычисление скорости изменения кватерниона
            qDot1 = 0.5 * (-q2 * gx - q3 * gy - q4 * gz) - Beta * s1;
            qDot2 = 0.5 * (q1 * gx + q3 * gz - q4 * gy) - Beta * s2;
            qDot3 = 0.5 * (q1 * gy - q2 * gz + q4 * gx) - Beta * s3;
            qDot4 = 0.5 * (q1 * gz + q2 * gy - q3 * gx) - Beta * s4;

            //  Интегрирование для получения кватерниона
            q1 += qDot1 * SamplePeriod;
            q2 += qDot2 * SamplePeriod;
            q3 += qDot3 * SamplePeriod;
            q4 += qDot4 * SamplePeriod;

            // Нормализация кватерниона
            norm = 1.0 / (double)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);
            Quaternion[0] = q1 * norm;
            Quaternion[1] = q2 * norm;
            Quaternion[2] = q3 * norm;
            Quaternion[3] = q4 * norm;
            /*
            double x = g[0];
            double y = g[1];
            double z = g[2];
            double w = g[3];
            */

            //Set values to commonly known quaternion letter representatives
            double x = Quaternion[0];
            double y = Quaternion[1];
            double z = Quaternion[2];
            double w = Quaternion[3];
            //x w, 
            //  Pitch= Angles[0], Tilt = Angles[1], Azimuth = Angles[2];

            double sinP = 2.0 * (w * x + y * z);
            double cosP = 1.0 - 2.0 * (x * x + y * y);
            double sinT = 2.0 * (w * y - z * x);
            double sinA = 2.0 * (w * z + x * y);
            double cosA = 1.0 - 2.0 * (y * y + z * z);

            Angles[0] = Math.Atan2(sinP, cosP) * (180 / Math.PI);
            if (Math.Abs(sinT) >= 1)
            {
                Angles[1] = Math.PI / 2 * (180 / Math.PI);
            }
            else
                Angles[1] = Math.Asin(sinT) * (180 / Math.PI);

            Angles[2] = Math.Atan2(sinA, cosA) * (180 / Math.PI);

            // string writePath = @"C:\SomeDir\hta.txt";

            // string text = "Привет мир!\nПока мир...";

            // using (StreamWriter sw = new StreamWriter(writePath, false, System.Text.Encoding.Default)
            //{
            //       sw.WriteLine(text);     
            // } 
        }
    }
}

