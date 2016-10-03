using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars;
using System.Collections;
using TMDVD.DataType.Interface;
using System.Reflection;

namespace TDC
{
    public partial class Main : DevExpress.XtraBars.Ribbon.RibbonForm
    {

        TMDVD.Logic.DVDController cnt;
        bool started = false;

        public Main()
        {
            InitializeComponent();
        }

        private void Start()
        {

            cnt = new TMDVD.Logic.DVDController("configfile.xml");

            cnt.Initialize();
        }

        private void barStartItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!started)
            {
                Start();

                //load supplayer
                bsSupplayer.DataSource = new BindingList<ISupplier>(cnt.GetAllSuppliersV1());
                bsSupplayer.PositionChanged += BsSupplayer_PositionChanged;
                started = true;
            }
           
        }

        private void BsSupplayer_PositionChanged(object sender, EventArgs e)
        {
            FieldInfo fo = typeof(TMDVD.Logic.DVDController).GetField("bdfArticleDAL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            IEnumerable<IArticle> en = (IEnumerable<IArticle>)fo.FieldType.GetMethod("GetAllArticles").Invoke(fo.GetValue(cnt), new object[] { bsSupplayer.Current as ISupplier });

            bsArticle.DataSource = en.Take(1000);
            xtraTabControl1.SelectedTabPage = xtraTabPage2;
        }
    }
}