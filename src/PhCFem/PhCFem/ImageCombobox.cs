using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PhCFem
{
    /// <summary>
    /// イメージコンボボックス
    /// </summary>
    public partial class ImageCombobox : ComboBox
    {
        /// <summary>
        /// イメージリスト
        /// </summary>
        public ImageList ImageList
        {
            get;
            set;
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ImageCombobox()
        {
            InitializeComponent();
            this.DrawMode = DrawMode.OwnerDrawFixed;
            ImageList = new ImageList();
            this.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        /// <summary>
        /// アイテム描画イベントハンドラ
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawFocusRectangle();
            Rectangle bounds = e.Bounds;
            string s = "";
            if (this.Items != null && e.Index >= 0 && e.Index < this.Items.Count)
            {
                s = this.Items[e.Index].ToString();
            }
            try
            {
                if (ImageList != null && ImageList.Images.Count != 0 && e.Index >= 0 && e.Index < this.Items.Count)
                {
                    ImageList.Draw(e.Graphics, bounds.Left, bounds.Top, e.Index);
                    using (Brush brush = new SolidBrush(e.ForeColor))
                    {
                        e.Graphics.DrawString(s, e.Font, brush, bounds.Left + ImageList.Images[e.Index].Width, bounds.Top);
                    }
                }
                else
                {
                    using (Brush brush = new SolidBrush(e.ForeColor))
                    {
                        e.Graphics.DrawString(s, e.Font, brush, bounds.Left, bounds.Top);
                    }
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception.Message + " " + exception.StackTrace);
                using (Brush brush = new SolidBrush(e.ForeColor))
                {
                    e.Graphics.DrawString(s, e.Font, brush, bounds.Left, bounds.Top);
                }
            }

            base.OnDrawItem(e);
        }
    }
}