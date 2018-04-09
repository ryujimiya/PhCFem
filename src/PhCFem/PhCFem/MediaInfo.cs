using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PhCFem
{
    /// <summary>
    /// 媒質情報
    /// </summary>
    class MediaInfo : ICloneable
    {
        public double[,] P
        {
            get { return (double[,])p.Clone(); }
        }

        public double[,] Q
        {
            get { return (double[,])q.Clone(); }
        }

        public Color BackColor
        {
            get;
            set;
        }

        public MediaInfo()
        {
            p = new double[,]
            {
                {1.0, 0.0, 0.0},
                {0.0, 1.0, 0.0},
                {0.0, 0.0, 1.0},
            };
            q = new double[,]
            {
                {1.0, 0.0, 0.0},
                {0.0, 1.0, 0.0},
                {0.0, 0.0, 1.0},
            };
            BackColor = Color.Gray;
        }

        public MediaInfo(double[,] p_, double[,] q_)
        {
            System.Diagnostics.Debug.Assert(p_.GetLength(0) == 3);
            System.Diagnostics.Debug.Assert(p_.GetLength(1) == 3);
            System.Diagnostics.Debug.Assert(q_.GetLength(0) == 3);
            System.Diagnostics.Debug.Assert(q_.GetLength(1) == 3);
            p = (double[,])p_.Clone();
            q = (double[,])q_.Clone();
            BackColor = Color.Gray;
        }

        public void SetP(double[,] p_)
        {
            System.Diagnostics.Debug.Assert(p_.GetLength(0) == 3);
            System.Diagnostics.Debug.Assert(p_.GetLength(1) == 3);
            p = (double[,])p_.Clone();
        }

        public void SetQ(double[,] q_)
        {
            System.Diagnostics.Debug.Assert(q_.GetLength(0) == 3);
            System.Diagnostics.Debug.Assert(q_.GetLength(1) == 3);
            q = (double[,])q_.Clone();
        }

        public override string ToString()
        {
            string tmp;
            tmp = "P:" + System.Environment.NewLine
                  + "    " + P[0, 0] + " " + P[0, 1] + " " + P[0, 2] + System.Environment.NewLine
                  + "    " + P[1, 0] + " " + P[1, 1] + " " + P[1, 2] + System.Environment.NewLine
                  + "    " + P[2, 0] + " " + P[2, 1] + " " + P[2, 2] + System.Environment.NewLine
                  + "Q:" + System.Environment.NewLine
                  + "    " + Q[0, 0] + " " + Q[0, 1] + " " + Q[0, 2] + System.Environment.NewLine
                  + "    " + Q[1, 0] + " " + Q[1, 1] + " " + Q[1, 2] + System.Environment.NewLine
                  + "    " + Q[2, 0] + " " + Q[2, 1] + " " + Q[2, 2] + System.Environment.NewLine
                  ;
            return tmp;
        }

        public object Clone()
        {
            MediaInfo media = new MediaInfo(this.p, this.q);
            media.BackColor = this.BackColor;
            return (object)media;
        }

        private double[,] p;
        private double[,] q;
    }

}
