using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexturePacker
{
    public enum FreeRectChoiceHeuristic
    {
        RectBestAreaFit,
        RectBottomLeftRule,
        RectBestShortSideFit,
    }

    public class MaxRectsBinPacker
    {
        public MaxRectsBinPacker(int width, int height)
        {
            freeRects = new List<Rect>();
            usedRects = new List<Rect>();

            freeRects.Add(new Rect(0, 0, width, height));
        }

        public Rect Insert(int width, int height, FreeRectChoiceHeuristic method = FreeRectChoiceHeuristic.RectBottomLeftRule)
        {
            Rect newNode = null;

            switch(method)
            {
                case FreeRectChoiceHeuristic.RectBottomLeftRule:
                    newNode = FindPositionForBottomLeft(width, height);
                    break;
                case FreeRectChoiceHeuristic.RectBestAreaFit:
                    newNode = FindPositionForBestArea(width, height);
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (Rect.IsEmpty(newNode))
                return newNode;

            int freeSize = freeRects.Count;
            for(int i = 0; i < freeSize; ++i)
            {
                if (SplitFreeNode(freeRects[i], newNode))
                {
                    freeRects.Remove(freeRects[i]);
                    --i;
                    --freeSize;
                }
            }

            PruneFreeList();

            usedRects.Add(newNode);

            return newNode;
        }

        private void PruneFreeList()
        {
            for(int i = 0; i < freeRects.Count; ++i)
            {
                for(int j = i + 1; j < freeRects.Count; ++j)
                {
                    if (Rect.IsContainIn(freeRects[i], freeRects[j]))
                    {
                        freeRects.RemoveAt(i);
                        --i;
                        break;
                    }

                    if (Rect.IsContainIn(freeRects[j], freeRects[i]))
                    {
                        freeRects.RemoveAt(j);
                        --j;
                    }
                }
            }
        }

        private bool SplitFreeNode(Rect freeNode, Rect newNode)
        {
            //探测是否相交
            if (!Rect.IsIntersected(newNode, freeNode))
                return false;

            //y方向
            if (newNode.x < freeNode.x + freeNode.width && newNode.x + newNode.width > freeNode.x)
            {
                //位于上方
                if (newNode.y > freeNode.y && newNode.y < freeNode.y + freeNode.height)
                {
                    Rect newFreeNode = new Rect(freeNode.x, freeNode.y, freeNode.width, newNode.y - freeNode.y);
                    freeRects.Add(newFreeNode);
                }

                //下方
                if (newNode.y + newNode.height < freeNode.y + freeNode.height)
                {
                    Rect newFreeNode = new Rect(freeNode.x, newNode.y + newNode.height, freeNode.width, freeNode.y + freeNode.height - newNode.height - newNode.y);
                    freeRects.Add(newFreeNode);
                }
            }

            //x方向
            if (newNode.y < freeNode.y + freeNode.height && newNode.y + newNode.height > freeNode.y)
            {
                //right
                if (newNode.x > freeNode.x && newNode.x < freeNode.x + freeNode.width)
                {
                    Rect newFreeNode = new Rect(freeNode.x, freeNode.y, newNode.x - freeNode.x, freeNode.height);
                    freeRects.Add(newFreeNode);
                }

                if (newNode.x + newNode.width < freeNode.x + freeNode.width)
                {
                    Rect newFreeNode = new Rect(newNode.x + newNode.width, freeNode.y, freeNode.x + freeNode.width - newNode.x - newNode.width, freeNode.height);
                    freeRects.Add(newFreeNode);
                }
            }

            return true;
        }

        private Rect FindPositionForBestArea(int width, int height)
        {
            Rect bestNode = new Rect();
            int bestAreaFit = int.MaxValue;
            int bestShortSideFit = int.MaxValue;

            for (int i = 0; i != freeRects.Count; ++i)
            {
                int areaFit = freeRects[i].width * freeRects[i].height - width * height;

                if (freeRects[i].width >= width && freeRects[i].height >= height)
                {
                    int leftOverHoriz = Math.Abs(freeRects[i].width - width);
                    int leftOverVert = Math.Abs(freeRects[i].height - height);
                    int shorSideFit = Math.Min(leftOverHoriz, leftOverVert);

                    if (areaFit < bestAreaFit || (areaFit == bestAreaFit && shorSideFit < bestShortSideFit))
                    {
                        bestNode.x = freeRects[i].x;
                        bestNode.y = freeRects[i].y;
                        bestNode.width = width;
                        bestNode.height = height;
                        bestAreaFit = areaFit;
                        bestShortSideFit = shorSideFit;
                    }
                }
            }

            return bestNode;
        }

        private Rect FindPositionForBottomLeft(int width, int height)
        {
            //规则->
            //newNode -》 min(x) min(y) 先放置在y最小的位置 如果y一样 那么再比较x最小

            Rect bestNode = new Rect();
            int bestY = int.MaxValue;
            int bestX = int.MaxValue;

            foreach(var freeNode in freeRects)
            {
                //能够放下
                if (freeNode.height >= height && freeNode.width >= width)
                {
                    int topY = freeNode.y + height;

                    if (topY < bestY || (topY == bestY && freeNode.x < bestX))
                    {
                        bestNode.x = freeNode.x;
                        bestNode.y = freeNode.y;
                        bestNode.width = width;
                        bestNode.height = height;
                        bestY = topY;
                        bestX = freeNode.y;
                    }
                }
            }

            return bestNode;
        }

        public List<Rect> freeRects;
        public List<Rect> usedRects;
    }
}
