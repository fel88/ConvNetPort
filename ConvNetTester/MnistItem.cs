using ConvNetLib;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ConvNetTester
{
    public class MnistItem
    {
        public static int NewId;
        public MnistItem()
        {

            Id = NewId++;
            if (Id == 834)
            {

            }
        }
        public int Id;
        public byte[,] Data;
        public int Label;
        private ReadOnlyBitmap _bitmap = null;

        public ReadOnlyBitmap Bitmap
        {
            get
            {
                if (_bitmap == null)
                {
                    _bitmap = GetBitmap();
                }
                return _bitmap;
            }
            set
            {
                _bitmap = value;
            }
        }
        public ReadOnlyBitmap GetBitmap()
        {
            if (_bitmap != null) return _bitmap;
            Bitmap bmp = new Bitmap(Data.GetLength(0), Data.GetLength(1));
            ReadOnlyBitmap rom = new ReadOnlyBitmap(bmp);
            for (int i = 0; i < Data.GetLength(0); i++)
            {
                for (int j = 0; j < Data.GetLength(1); j++)
                {
                    rom.SetPixel(i, j, new[] { Data[i, j], Data[i, j], Data[i, j], (byte)255 });
                }
            }
            _bitmap = rom;
            return _bitmap;
        }
    }

    public class MnistSampleItem
    {
        public int label
        {
            get
            {
                return Item.Label;
            }
        }
        public object[] x;
        public MnistItem Item;
    }
    public class MnistItemVolumePrepared
    {
        public Volume x;
        public int label;
        public bool isVal;
        public object[] raw;
        public bool isval;
    }

    public class CifarVolumePrepared
    {
        public Volume x;
        public int label;
        public bool isVal;
        public object[] raw;
        public bool isval;
        public CifarImg Item;
    }
    public class CifarStuff
    {
        public static bool use_validation_data = true;
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
            return new CifarVolumePrepared() { x = x, label = item.label, isval = isval, Item = item };
        }
        public static List<CifarImg> items = new List<CifarImg>();

        public static List<CifarImg> tests = new List<CifarImg>();
        public static Random Rand = new Random();
        public static string[] labels;

        public static CifarImg random_one()
        {
            return items[Rand.Next(items.Count)];
        }
    }
    public class MnistStuff
    {
        public static Random Rand = new Random();
        public static int num_samples_per_batch = 3000;
        public static int num_batches = 21;
        public static int step_num = 0;
        private static bool[] loaded = new bool[1000];
        private static int[] loaded_train_batches = new int[1000];

        public static void load_data_batch(int i)
        {

        }

        public static bool use_validation_data = true;
        public static MnistItem lastitem = null;
        public static MnistItemVolumePrepared sample_training_instance()
        {

            // find an unloaded batch
            /*  var bi = (int)Math.Floor(Rand.NextDouble() * loaded_train_batches.Count());
              var b = loaded_train_batches[bi];
              var k = (int)Math.Floor(Rand.NextDouble() * num_samples_per_batch); // sample within the batch
              var n = b * num_samples_per_batch + k;*/
            var n = (int)(Rand.NextDouble() * items.Count);

            lastitem = items[n];
            // load more batches over time
            if (step_num % (2 * num_samples_per_batch) == 0 && step_num > 0)
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
            }
            var image_dimension = items[n].Bitmap.Width;
            var image_channels = 1;
            // fetch the appropriate row of the training image and reshape into a Vol

            var x = new Volume(image_dimension, image_dimension, image_channels, 0.0);
            var W = image_dimension * image_dimension;
            var j = 0;
            for (var dc = 0; dc < image_channels; dc++)
            {
                var i = 0;
                for (var xc = 0; xc < image_dimension; xc++)
                {
                    for (var yc = 0; yc < image_dimension; yc++)
                    {
                        // var ix = ((W * k) + i) * 4 + dc;
                        var bb = items[n].Bitmap.GetPixel(xc, yc);
                        //x.Set(yc, xc, dc, p[ix]/255.0 - 0.5);
                        x.Set(yc, xc, dc, bb / 255.0 - 0.5);
                        i++;
                    }
                }
            }

            if (random_position)
            {
                var dx = (int)Math.Floor(Rand.NextDouble() * 5 - 2);
                var dy = (int)Math.Floor(Rand.NextDouble() * 5 - 2);
                x = Volume.Augment(x, image_dimension, dx, dy, false); //maybe change position
            }

            if (random_flip)
            {
                x = Volume.Augment(x, image_dimension, 0, 0, Rand.NextDouble() < 0.5); //maybe flip horizontally
            }

            var isval = use_validation_data && n % 10 == 0;
            return new MnistItemVolumePrepared() { x = x, label = items[n].Label, isVal = isval };
        }

        private static bool random_position;
        private static bool random_flip;
        public static List<MnistItem> items = new List<MnistItem>();
        public static List<MnistItem> tests = new List<MnistItem>();
        public static int test_batch;

        // sample a random testing instance
        public static MnistSampleItem sample_test_instance()
        {

            var n = (int)(Rand.NextDouble() * tests.Count);

            /*   var b = test_batch;
               var k = (int)Math.Floor(Rand.NextDouble() * num_samples_per_batch);
               var n = b * num_samples_per_batch + k;*/
            var item = tests[n];

            var image_dimension = item.Bitmap.Width;
            var image_channels = 1;

            var x = new Volume(image_dimension, image_dimension, image_channels, 0.0);
            var W = image_dimension * image_dimension;
            var j = 0;
            for (var dc = 0; dc < image_channels; dc++)
            {
                var i = 0;
                for (var xc = 0; xc < image_dimension; xc++)
                {
                    for (var yc = 0; yc < image_dimension; yc++)
                    {
                        // var ix = ((W * k) + i) * 4 + dc;
                        var bb = item.Bitmap.GetPixel(xc, yc);
                        //x.Set(yc,xc,dc,p[ix]/255.0-0.5);
                        x.Set(yc, xc, dc, bb / 255.0 - 0.5);
                        i++;
                    }
                }
            }

            // distort position and maybe flip
            var xs = new List<object>();

            if (random_flip || random_position)
            {
                for (var k = 0; k < 6; k++)
                {
                    var test_variation = x;
                    if (random_position)
                    {
                        var dx = (int)Math.Floor(Rand.NextDouble() * 5 - 2);
                        var dy = (int)Math.Floor(Rand.NextDouble() * 5 - 2);
                        test_variation = Volume.Augment(test_variation, image_dimension, dx, dy, false);
                    }

                    if (random_flip)
                    {
                        test_variation = Volume.Augment(test_variation, image_dimension, 0, 0, Rand.NextDouble() < 0.5);
                    }

                    xs.Add(test_variation);
                }
            }
            else
            {
                xs.Add(x);
                xs.Add(image_dimension);
                xs.Add(0);
                xs.Add(0);
                xs.Add(false);
                //xs.Add(x, image_dimension, 0, 0, false); // push an un-augmented copy
            }

            // return multiple augmentations, and we will average the network over them
            // to increase performance
            //return new MnistItemVolumePrepared() {x = xs; label=labels[n]};
            return new MnistSampleItem()
            {
                x = xs.ToArray(),
                Item = item
            };

        }


    }

    public class CifarImg
    {
        public Bitmap Bmp;

        public byte label;
        public Volume x;
        internal bool isval;
    }
}