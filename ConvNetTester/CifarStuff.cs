using ConvNetLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace ConvNetTester
{
    public class CifarStuff
    {
        public static bool use_validation_data = true;
        public static CifarVolumePrepared sample_test_instance()
        {


            var k = (int)Math.Floor(Rand.NextDouble() * tests.Count); // sample within the batch

            // fetch the appropriate row of the training image and reshape into a Vol
            var item = tests[k];
            var p = item.Bmp;
            var x = new Volume(32, 32, 3, 0.0);
            var W = 32 * 32;
            var j = 0;
            for (var dc = 0; dc < 3; dc++)
            {
                var i = 0;
                for (var xc = 0; xc < 32; xc++)
                {

                    for (var yc = 0; yc < 32; yc++)
                    {
                        var px = p.GetPixel(xc, yc);
                        var bt = (byte)((px.ToArgb() & (dc << 8)) >> 8);
                        var ix = ((W * k) + i) * 4 + dc;
                        x.Set(yc, xc, dc, bt / 255.0 - 0.5);
                        i++;
                    }
                }
            }

            var dx = (int)Math.Floor(Rand.NextDouble() * 5 - 2);
            var dy = (int)Math.Floor(Rand.NextDouble() * 5 - 2);

            x = Volume.Augment(x, 32, dx, dy, Rand.NextDouble() < 0.5); //maybe flip horizontally
            item.x = x;
            return item;
        }
        public static CifarVolumePrepared sample_training_instance()
        {

            // find an unloaded batch
            //var bi = Math.Floor(Rand.NextDouble() * loaded_train_batches.length);
            // var b = loaded_train_batches[bi];
            var k = (int)Math.Floor(Rand.NextDouble() * items.Count); // sample within the batch
            /*var n = b * 1000 + k;

            // load more batches over time
            if (step_num % 2000 == 0 && step_num > 0)
            {
                for (var i = 0; i < num_batches; i++)
                {
                    if (!loaded[i])
                    {
                        // load it
                        load_data_batch(i);
                        break; // okay for now
                    }
                }
            }*/

            // fetch the appropriate row of the training image and reshape into a Vol
            var item = items[k];

            var p = item.Bmp;
            var x = new Volume(32, 32, 3, 0.0);
            var W = 32 * 32;
            var j = 0;
            for (var dc = 0; dc < 3; dc++)
            {
                var i = 0;
                for (var xc = 0; xc < 32; xc++)
                {

                    for (var yc = 0; yc < 32; yc++)
                    {
                        var px = p.GetPixel(xc, yc);
                        var bt = (byte)((px.ToArgb() & (dc << 8)) >> 8);
                        var ix = ((W * k) + i) * 4 + dc;
                        x.Set(yc, xc, dc, bt / 255.0 - 0.5);
                        i++;
                    }
                }
            }

            var dx = (int)Math.Floor(Rand.NextDouble() * 5 - 2);
            var dy = (int)Math.Floor(Rand.NextDouble() * 5 - 2);

            x = Volume.Augment(x, 32, dx, dy, Rand.NextDouble() < 0.5); //maybe flip horizontally

            var isval = use_validation_data && k % 10 == 0 ? true : false;
            item.x = x;
            item.isval = isval;

            return item;
        }
        public static List<CifarVolumePrepared> items = new List<CifarVolumePrepared>();

        public static List<CifarVolumePrepared> tests = new List<CifarVolumePrepared>();
        public static Random Rand = new Random();
        public static string[] labels;

        
    }
    public class CifarVolumePrepared
    {
        public Volume x;
        public int label;
        public bool isVal;
        public object[] raw;
        public bool isval;
        public Bitmap Bmp;
    }
}
