using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexturePacker
{
    public class Rect
    {
        public Rect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public Rect()
        {
            this.x = 0;
            this.y = 0;
            this.width = 0;
            this.height = 0;
        }

        //test a is Contained in b
        public static bool IsContainIn(Rect a, Rect b)
        {
            return a.x >= b.x && a.y >= b.y && a.x + a.width <= b.x + b.width && a.y + a.height <= b.y + b.height;
        }

        public static bool IsIntersected(Rect a, Rect b)
        {
            if (a.x >= b.x + b.width || a.x + a.width <= b.x || a.y >= b.y + b.height || a.y + a.height <= b.y)
                return false;

            return true;
        }

        public static bool IsEmpty(Rect a)
        {
            return a.height == 0;
        }

        public int x;
        public int y;
        public int width;
        public int height;
    }
}
