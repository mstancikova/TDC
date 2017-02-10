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

using TMDVD.DAL.BDF.ArticleDataFiles;
using TMDVD.DAL.BDF.CommonData;
using TMDVD.DAL.BDF.DataType;
using TMDVD.DataType.Common;
using TMDVD.DataType.Export;

using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Columns;
using DevExpress.XtraTreeList.Nodes;

//prepisovanie binary .net : http://resources.infosecinstitute.com/patching-net-binary-code-with-cff-explorer/#gref

namespace TDC
{
	public partial class Main : DevExpress.XtraBars.Ribbon.RibbonForm
	{

		TMDVD.Logic.DVDController cnt;
		bool started = false;
		int[] captionLevels = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		int iCaptionIterator = 0;

		public Main()
		{
			InitializeComponent();
			this.Width = 1200;
			this.Height = 900;
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
				started = true;
			}

		}

		private void log(string text, bool clear = false)
		{
			if (clear) richTextBox2.Clear();

			if (richTextBox2.Lines.Count() > 0) richTextBox2.AppendText("\n" + text);
			else richTextBox2.AppendText(text);

			richTextBox2.SelectionStart = richTextBox2.Text.Length;
			richTextBox2.ScrollToCaret();
		}


		private void BsSupplayer_PositionChanged(object sender, EventArgs e)
		{
			//GetAllArticles by supplier
			FieldInfo fo = typeof(TMDVD.Logic.DVDController).GetField("bdfArticleDAL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			IEnumerable<IArticle> en = (IEnumerable<IArticle>)fo.FieldType.GetMethod("GetAllArticles").Invoke(fo.GetValue(cnt), new object[] { bsSupplayer.Current as ISupplier });
			bsArticle.DataSource = en.Take(10000);
		}

		private void updateCaption(int iLevel)
		{
			iCaptionIterator++;
			captionLevels[iLevel]++;

			if ((iCaptionIterator % 10000) == 1)
			{
				this.Text = "Level0=" + captionLevels[0] + " Level1=" + captionLevels[1] + " Level2=" + captionLevels[2] + " Level3=" + captionLevels[3] + " Level4=" + captionLevels[4] + " Level5=" + captionLevels[5] + " Level6=" + captionLevels[6] + " Level7=" + captionLevels[7] + " Level8=" + captionLevels[8] + " Level9=" + captionLevels[9];
				Application.DoEvents();
			}
		}

		private void showObject(string label, object obj, int iLevel, TreeListNode node, Stack path)
		{
			if (obj == null) return;

			if (iLevel == 0) 
			{
				iCaptionIterator = 0;
				captionLevels = new int[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			}
			
			if (iLevel > 6) return;
			foreach (object o in path) if (ReferenceEquals(o, obj)) return;

			updateCaption(iLevel);

			if (obj.GetType().IsPrimitive || (obj as string) != null)
			{
				node.Nodes.Add(new object[] { label, obj.ToString() });
				testObject(obj);
				return;
			}

			path.Push(obj);

			if ((obj as IDictionary) != null)
			{
				TreeListNode lastnode = node.Nodes.Add(new object[] { label, "list" });
				foreach (System.Collections.DictionaryEntry item in obj as IDictionary)
				{
					showObject(item.Key + "[" + item.Key.GetType().Name + "]", item.Value, iLevel + 1, lastnode, path);
				}
			}
			else
			if ((obj as IList) != null)
			{
				TreeListNode lastnode = node.Nodes.Add(new object[] { label, "list" });
				foreach (var prop2 in obj as IList)
				{
					showObject(prop2.ToString() + "[" + prop2.GetType().Name + "]", prop2, iLevel + 1, lastnode, path);
				}
			}
			else
			if ((obj as IEnumerable) != null)
			{
				TreeListNode lastnode = node.Nodes.Add(new object[] { label, "Enumerable" });
				foreach (var prop2 in obj as IEnumerable)
				{
					showObject(prop2.ToString() + "[" + prop2.GetType().Name + "]", prop2, iLevel + 1, lastnode, path);
				}
			}
			else 
			{
				TreeListNode lastNode = node.Nodes.Add(new object[] { label, "Object" });

				PropertyInfo[] x1 = obj.GetType().GetProperties();// System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public); // | System.Reflection.BindingFlags.Public);
				foreach (var prop3 in x1)
				{
					try 
					{
						//if (prop3.PropertyType.IsGenericParameter == true)
						{
							Object value = prop3.GetValue(obj);
							if (value != null)
								showObject(prop3.Name, value, iLevel + 1, lastNode, path);
						}
					}
					catch (Exception e) 
					{
						Console.WriteLine(e.Message);
					}
				}				
			}
			path.Pop();
		}

		private void createLinkItemTree(IArticle article)
		{
			treeList1.Nodes.Clear();
			treeList1.BeginUpdate();
			TreeListNode rootNode = treeList1.Nodes.Add(new Object[] { "root", "" });
			IDictionary<ESearchTreeType, IList<ILinkitem>> tmpMap = cnt.GetLinkedItemsV1(article) as IDictionary<ESearchTreeType, IList<ILinkitem>>;
			showObject("Articles", tmpMap, 0, rootNode, new Stack());
			treeList1.EndUpdate();
		}

		private void BsArticle_PositionChanged(object sender, EventArgs e)
		{

			FieldInfo fo1 = typeof(TMDVD.Logic.DVDController).GetField("bdfArticleDAL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			FieldInfo fo2 = typeof(TMDVD.DAL.BDF.ArticleData).GetField("<BdfArticleDetails>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			//Article detail
			object o = fo1.GetValue(cnt);
			object o1 = fo2.GetValue(o);
			MethodInfo mi = fo2.FieldType.GetMethod("GetArticleDetails", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			object o3 = mi.Invoke(o1, new object[] { bsArticle.Current as IArticle, 65535 });

			IArticleDetails en = o3 as IArticleDetails;
			//FieldInfo fo4 = typeof(TMDVD.DAL.BDF.ArticleData).GetField("dalMasterdata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			//TMDVD.DAL.BDF.MasterData o4 = fo4.GetValue(o) as TMDVD.DAL.BDF.MasterData;
			//bool bb = o4.IsValid();
			//object fo5 = typeof(TMDVD.DAL.BDF.DataType.Security);//.GetField("dalMasterdata", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			//TMDVD.DAL.BDF.DataType.Security

            if(en != null)
            {
                bsOEM.DataSource = en.OENumbers;

                bsCross.DataSource = cnt.SearchArticleV1(en.OENumbers.First().OENbr);

            }

			if (en != null)
			{
				richTextBox1.Clear();
				richTextBox1.AppendText("Article : " + en.Article.ToString());
				richTextBox1.AppendText("\nHasAxle : " + en.HasAxle.ToString());
				richTextBox1.AppendText("\nHasCommercialVehicle : " + en.HasCommercialVehicle.ToString());
				richTextBox1.AppendText("\nHasCVManuID : " + en.HasCVManuID.ToString());
				richTextBox1.AppendText("\nHasEngine : " + en.HasEngine.ToString());
				richTextBox1.AppendText("\nHasLinkitems : " + en.HasLinkitems.ToString());
				richTextBox1.AppendText("\nHasMotorbike : " + en.HasMotorbike.ToString());
				richTextBox1.AppendText("\nHasPassengerCar : " + en.HasPassengerCar.ToString());
			}
			else
				richTextBox1.AppendText("There is not ArticleDetails for article !");


			//TAF24Prices
			IEnumerable<TAF24Prices> pr = cnt.GetTAF24Prices(bsArticle.Current as IArticle);
			log("TAF24Prices count : " + pr.Count());
			bsTAF24Prices.DataSource = pr.Take(100);

			//AllProducts
			IList<IProduct> AllProducts = (bsArticle.Current as IArticle).AllProducts;
			log("All product for article : " + AllProducts.Count);
			bsAllProducts.DataSource = AllProducts.Take(100);

			//ILinkitemAttribute
			ILinkitemAttribute liAttribute = (bsArticle.Current as IArticle).ArticleState;
			if (liAttribute != null)
			{
				richTextBox3.Clear();
				richTextBox3.AppendText("AttributeGroup : " + liAttribute.AttributeGroup.ToString());
				richTextBox3.AppendText("\nAttributeType : " + liAttribute.AttributeType.ToString());
				richTextBox3.AppendText("\nDisplayTitle : " + liAttribute.DisplayTitle.ToString());
				richTextBox3.AppendText("\nDisplayValue : " + liAttribute.DisplayValue.ToString());
			}
			else
				richTextBox3.AppendText("There is not LinkitemAttribute for article !");

			//IAttribute
			IList<IAttribute> Attributes = (bsArticle.Current as IArticle).Attributes;
			log("Attributes for article : " + Attributes.Count);
			bsAttributes.DataSource = Attributes.Take(1000);


			//IProduct
			IProduct product = (bsArticle.Current as IArticle).CurrentProduct;
			if (product != null)
			{
				richTextBox4.Clear();
				richTextBox4.AppendText("AssemblyGroupDescription : " + product.AssemblyGroupDescription.ToString());
				richTextBox4.AppendText("\nDescription : " + product.Description.ToString());
				richTextBox4.AppendText("\nID : " + product.ID.ToString());
				richTextBox4.AppendText("\nNormalizedDescription : " + product.NormalizedDescription.ToString());
				richTextBox4.AppendText("\nUsageDescription : " + product.UsageDescription.ToString());
			}
			else
				richTextBox4.AppendText("There is not current product for article !");

			//Informations
			IList<IArticleInformation> Informations = (bsArticle.Current as IArticle).Informations;
			log("Informations for article : " + Informations.Count);
			bsArticleInformation.DataSource = Informations.Take(1000);

			//IMediaInformation
			IList<IMediaInformation> MediaInformations = (bsArticle.Current as IArticle).MediaInformations;
			log("Media Informations for article : " + MediaInformations.Count);
			bsMediaInformation.DataSource = MediaInformations.Take(1000);

			//IArticleNewNbr
			IList<IArticleNewNbr> ArticleNewNbr = (bsArticle.Current as IArticle).NewNumbers;
			log("New numberss for article : " + ArticleNewNbr.Count);
			bsArticleNewNbr.DataSource = ArticleNewNbr.Take(1000);

			//IArticlePartsList
			IArticlePartsList ArticlePartsList = (bsArticle.Current as IArticle).PartsList;
			if (ArticlePartsList != null)
			{
				IList<IPartListArticle> PartListArticle = ArticlePartsList.Articles;
				log("Article parts list : " + PartListArticle.Count);
				bsPartLitArticle.DataSource = PartListArticle.Take(1000);
			}

			//IArticlePrice
			IList<IArticlePrice> Prices = (bsArticle.Current as IArticle).Prices;
			log("Prices for article : " + Prices.Count);
			bsPrices.DataSource = Prices.Take(1000);

			//IArticleReplaceNbr
			IList<IArticleReplaceNbr> ReplaceNbr = (bsArticle.Current as IArticle).ReplaceNumbers;
			log("ReplaceNbr for article : " + ReplaceNbr.Count);
			bsReplaceNumbers.DataSource = ReplaceNbr.Take(1000);

			//ISupplier
			ISupplier Supplier = (bsArticle.Current as IArticle).Supplier;
			if (Supplier != null)
			{
				IList<ISupplierDetail> SupplierDetail = Supplier.Addresses;
				log("Supplier detail for article : " + SupplierDetail.Count);
				bsSupplierDetail.DataSource = SupplierDetail.Take(1000);
				richTextBox5.Clear();
				richTextBox5.AppendText("\nDataVersion : " + Supplier.DataVersion.ToString());
				richTextBox5.AppendText("\nDescription : " + Supplier.Description.ToString());
				richTextBox5.AppendText("\nID : " + Supplier.ID.ToString());
				richTextBox5.AppendText("\nMatchCode : " + Supplier.MatchCode.ToString());
				richTextBox5.AppendText("\nNbrOfArticles : " + Supplier.NbrOfArticles.ToString());
			}
			else
				richTextBox5.AppendText("There is not Supplier for article !");

			//IArticleUtilityNbr
			IList<IArticleUtilityNbr> UtilityNbr = (bsArticle.Current as IArticle).UtilityNumbers;
			log("Utility Nbr for article : " + UtilityNbr.Count);
			bsUtilityNbr.DataSource = UtilityNbr.Take(1000);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			bsAllCountries.DataSource = new BindingList<ICountry>(cnt.GetAllCountriesV1());
		}

		private void button2_Click(object sender, EventArgs e)
		{
			bsAllCountryGroups.DataSource = new BindingList<ICountry>(cnt.GetAllCountryGroupsV1());
		}

		private void button3_Click(object sender, EventArgs e)
		{
			bsAllLanguages.DataSource = new BindingList<ILanguage>(cnt.GetAllLanguagesV1());
		}

		private void button4_Click(object sender, EventArgs e)
		{
			bsAllManufacturers.DataSource = new BindingList<IManufacturer>(cnt.GetAllManufacturersV1());
		}

		private void BsManufactures_PositionChanged(object sender, EventArgs e)
		{
			//For test to inspect manufacturer
			IManufacturer man = bsAllManufacturers.Current as IManufacturer;


			IEnumerable<IEngine> en = cnt.GetEnginesV1(bsAllManufacturers.Current as IManufacturer);
			log("Engines for current manufacturer : " + en.Count());
			bsEngines.DataSource = en.Take(1000);

			IEnumerable<IModel> mo = cnt.GetModelsV1(bsAllManufacturers.Current as IManufacturer);
			log("Models for current manufacturer : " + mo.Count());
			bsModels.DataSource = mo.Take(1000);

			if (bsModels.Count > bsEngines.Count) xtraTabControl9.SelectedTabPage = xtraTabPage47;
			else xtraTabControl9.SelectedTabPage = xtraTabPage46;
		}

		private void BsModels_PositionChanged(object sender, EventArgs e)
		{
			IEnumerable<IMotorbike> mo = cnt.GetMotorbikesV1(bsModels.Current as IModel);
			log("Motorbike for current model : " + mo.Count());
			bsMotorbikes.DataSource = mo.Take(1000);

			IEnumerable<IPassengerCar> pc = cnt.GetPassengerCarsV1(bsModels.Current as IModel);
			log("PassengerCar for current model : " + pc.Count());
			bsPassengerCars.DataSource = pc.Take(1000);

			IEnumerable<ICommercialVehicle> cv = cnt.GetCommercialVehiclesV1(bsModels.Current as IModel);
			log("CommercialVehicle for current model : " + cv.Count());
			bsCommercialVehicles.DataSource = cv.Take(1000);

			IEnumerable<IAxle> ax = cnt.GetAxlesV1(bsModels.Current as IModel);
			log("Acle for current model : " + ax.Count());
			bsAxles.DataSource = ax.Take(1000);

			if ((bsMotorbikes.Count > bsPassengerCars.Count) && (bsMotorbikes.Count > bsCommercialVehicles.Count) && 
				(bsMotorbikes.Count > bsAxles.Count)) xtraTabControl3.SelectedTabPage = xtraTabPage22;

			if ((bsPassengerCars.Count > bsMotorbikes.Count) && (bsPassengerCars.Count > bsCommercialVehicles.Count) &&
				(bsPassengerCars.Count > bsAxles.Count)) xtraTabControl3.SelectedTabPage = xtraTabPage23;

			if ((bsCommercialVehicles.Count > bsMotorbikes.Count) && (bsCommercialVehicles.Count > bsPassengerCars.Count) &&
				(bsCommercialVehicles.Count > bsAxles.Count)) xtraTabControl3.SelectedTabPage = xtraTabPage24;

			if ((bsAxles.Count > bsMotorbikes.Count) && (bsAxles.Count > bsPassengerCars.Count) &&
				(bsAxles.Count > bsCommercialVehicles.Count)) xtraTabControl3.SelectedTabPage = xtraTabPage25;

		}

		private void bsMotorbikes_PositionChanged(object sender, EventArgs e)
		{
			IMotorbike mo = bsMotorbikes.Current as IMotorbike;

			richTextBox6.Clear();
			richTextBox6.AppendText("\nCan be displayed : " + mo.CanBeDisplayed);
			richTextBox6.AppendText("\nConstructionInterval -> AttributeGroup : " + mo.ConstructionInterval.AttributeGroup.ToString());
			richTextBox6.AppendText("\nConstructionInterval -> DisplayTitle : " + mo.ConstructionInterval.DisplayTitle);
			richTextBox6.AppendText("\nConstructionInterval -> DisplayValue : " + mo.ConstructionInterval.DisplayValue);
			richTextBox6.AppendText("\nConstructionInterval -> From : " + mo.ConstructionInterval.From);
			richTextBox6.AppendText("\nConstructionInterval -> To : " + mo.ConstructionInterval.To);
			richTextBox6.AppendText("\nDescription : " + mo.Description);
			richTextBox6.AppendText("\nFullDescription : " + mo.FullDescription);
			richTextBox6.AppendText("\nHasLink : " + mo.HasLink);
			richTextBox6.AppendText("\nID : " + mo.ID);
			richTextBox6.AppendText("\nIsAxle : " + mo.IsAxle);
			richTextBox6.AppendText("\nIsCommercialVehicle : " + mo.IsCommercialVehicle);
			richTextBox6.AppendText("\nIsCVManufacturerID : " + mo.IsCVManufacturerID);
			richTextBox6.AppendText("\nIsEngine : " + mo.IsEngine);
			richTextBox6.AppendText("\nIsMotorbike : " + mo.IsMotorbike);
			richTextBox6.AppendText("\nIsPassengerCar : " + mo.IsPassengerCar);
			richTextBox6.AppendText("\nIsTransporter : " + mo.IsTransporter);
			richTextBox6.AppendText("\nIsValidForCurrentCountry : " + mo.IsValidForCurrentCountry);
			richTextBox6.AppendText("\nLinkitemType : " + mo.LinkitemType);

			IList<ILinkitemAttribute> liAttributes = mo.Attributes;
			log("LinkitemAttributes for current motorbike : " + liAttributes.Count);
			bsMotorbikeAttributes.DataSource = liAttributes;//.Take(1000);

			IModel model = mo.Model;
			if (model != null)
			{
				//Podla mna sa to odkazuje na to iste, cize master motorbiku je model.
				richTextBox7.Clear();
				richTextBox7.AppendText("\nCanBeDisplayed : " + model.CanBeDisplayed);
				richTextBox7.AppendText("\nConstructionInterval -> AttributeGroup : " + model.ConstructionInterval.AttributeGroup);
				richTextBox7.AppendText("\nConstructionInterval -> DisplayTitle : " + model.ConstructionInterval.DisplayTitle);
				richTextBox7.AppendText("\nConstructionInterval -> DisplayValue : " + model.ConstructionInterval.DisplayValue);
				richTextBox7.AppendText("\nConstructionInterval -> From : " + model.ConstructionInterval.From);
				richTextBox7.AppendText("\nConstructionInterval -> To : " + model.ConstructionInterval.To);
				richTextBox7.AppendText("\nDescription : " + model.Description);
				richTextBox7.AppendText("\nFullDescription : " + model.FullDescription);
				richTextBox7.AppendText("\nHasLink : " + model.HasLink);
				richTextBox7.AppendText("\nID : " + model.ID);
				richTextBox7.AppendText("\nIsAxle : " + model.IsAxle);
				richTextBox7.AppendText("\nIsCommercialVehicle : " + model.IsCommercialVehicle);
				richTextBox7.AppendText("\nIsCVManufacturerID : " + model.IsCVManufacturerID);
				richTextBox7.AppendText("\nIsEngine : " + model.IsEngine);
				richTextBox7.AppendText("\nIsMotorbike : " + model.IsMotorbike);
				richTextBox7.AppendText("\nIsPassengerCar : " + model.IsPassengerCar);
				richTextBox7.AppendText("\nIsTransporter : " + model.IsTransporter);
				richTextBox7.AppendText("\nIsValidForCurrentCountry : " + model.IsValidForCurrentCountry);
				richTextBox7.AppendText("\nLinkitemType : " + model.LinkitemType);

				IList<ILinkitemAttribute> liAttributes2 = model.Attributes;
				log("LinkitemAttributes for current motorbike -> model : " + liAttributes2.Count);
				bsMotorbikeModelAttributes.DataSource = liAttributes2.Take(1000);
			}
		}

		private void button5_Click(object sender, EventArgs e)
		{
			bsAllSuppliers.DataSource = new BindingList<ISupplier>(cnt.GetAllSuppliersV1());
		}

		private void button6_Click(object sender, EventArgs e)
		{
			bsCurrentDataCountries.DataSource = new BindingList<ICountry>(cnt.GetCurrentDataCountriesV1());
		}

		private void bsPassengerCars_PositionChanged(object sender, EventArgs e)
		{
			
			IPassengerCar pc = bsPassengerCars.Current as IPassengerCar;
			IList<ILinkitemAttribute> liAttributes = pc.Attributes;
			log("LinkitemAttributes for current passenger car : " + liAttributes.Count);
			bsPassengerCarAttributes.DataSource = liAttributes;

			richTextBox8.Clear();
			richTextBox8.AppendText("\nCan be displayed : " + pc.CanBeDisplayed);
			richTextBox8.AppendText("\nConstructionInterval -> AttributeGroup : " + pc.ConstructionInterval.AttributeGroup.ToString());
			richTextBox8.AppendText("\nConstructionInterval -> DisplayTitle : " + pc.ConstructionInterval.DisplayTitle);
			richTextBox8.AppendText("\nConstructionInterval -> DisplayValue : " + pc.ConstructionInterval.DisplayValue);
			richTextBox8.AppendText("\nConstructionInterval -> From : " + pc.ConstructionInterval.From);
			richTextBox8.AppendText("\nConstructionInterval -> To : " + pc.ConstructionInterval.To);
			richTextBox8.AppendText("\nDescription : " + pc.Description);
			richTextBox8.AppendText("\nFullDescription : " + pc.FullDescription);
			richTextBox8.AppendText("\nHasLink : " + pc.HasLink);
			richTextBox8.AppendText("\nID : " + pc.ID);
			richTextBox8.AppendText("\nIsAxle : " + pc.IsAxle);
			richTextBox8.AppendText("\nIsCommercialVehicle : " + pc.IsCommercialVehicle);
			richTextBox8.AppendText("\nIsCVManufacturerID : " + pc.IsCVManufacturerID);
			richTextBox8.AppendText("\nIsEngine : " + pc.IsEngine);
			richTextBox8.AppendText("\nIsMotorbike : " + pc.IsMotorbike);
			richTextBox8.AppendText("\nIsPassengerCar : " + pc.IsPassengerCar);
			richTextBox8.AppendText("\nIsTransporter : " + pc.IsTransporter);
			richTextBox8.AppendText("\nIsValidForCurrentCountry : " + pc.IsValidForCurrentCountry);
			richTextBox8.AppendText("\nLinkitemType : " + pc.LinkitemType);

			IList<IEngine> engines = pc.Engines;
			log("Engines for current passenger car : " + engines.Count);
			bsPassengerCarEngines.DataSource = engines;
				
		}

		private void bsCommercialVehicles_PositionChanged(object sender, EventArgs e)
		{
			
			ICommercialVehicle cv = bsCommercialVehicles.Current as ICommercialVehicle;
			IList<ILinkitemAttribute> liAttributes = cv.Attributes;
			log("LinkitemAttributes for current commercial vehicle : " + liAttributes.Count);
			bsCommercialVehiclesAttributes.DataSource = liAttributes;//.Take(1000);

			richTextBox9.Clear();
			richTextBox9.AppendText("\nCan be displayed : " + cv.CanBeDisplayed);
			richTextBox9.AppendText("\nConstructionInterval -> AttributeGroup : " + cv.ConstructionInterval.AttributeGroup.ToString());
			richTextBox9.AppendText("\nConstructionInterval -> DisplayTitle : " + cv.ConstructionInterval.DisplayTitle);
			richTextBox9.AppendText("\nConstructionInterval -> DisplayValue : " + cv.ConstructionInterval.DisplayValue);
			richTextBox9.AppendText("\nConstructionInterval -> From : " + cv.ConstructionInterval.From);
			richTextBox9.AppendText("\nConstructionInterval -> To : " + cv.ConstructionInterval.To);
			richTextBox9.AppendText("\nDescription : " + cv.Description);
			richTextBox9.AppendText("\nFullDescription : " + cv.FullDescription);
			richTextBox9.AppendText("\nHasLink : " + cv.HasLink);
			richTextBox9.AppendText("\nID : " + cv.ID);
			richTextBox9.AppendText("\nIsAxle : " + cv.IsAxle);
			richTextBox9.AppendText("\nIsCommercialVehicle : " + cv.IsCommercialVehicle);
			richTextBox9.AppendText("\nIsCVManufacturerID : " + cv.IsCVManufacturerID);
			richTextBox9.AppendText("\nIsEngine : " + cv.IsEngine);
			richTextBox9.AppendText("\nIsMotorbike : " + cv.IsMotorbike);
			richTextBox9.AppendText("\nIsPassengerCar : " + cv.IsPassengerCar);
			richTextBox9.AppendText("\nIsTransporter : " + cv.IsTransporter);
			richTextBox9.AppendText("\nIsValidForCurrentCountry : " + cv.IsValidForCurrentCountry);
			richTextBox9.AppendText("\nLinkitemType : " + cv.LinkitemType);

			IList<ICommercialVehicleAxle> cv_axles = cv.Axles;
			log("Axles for current commercial vehicle : " + cv_axles.Count);
			bsCommercialVehiclesAxles.DataSource = cv_axles;//.Take(1000);

			IList<ICVManufacturerID> cv_ids = cv.CVManufacturerIDs;
			log("CVManufacturerIDs for current commercial vehicle : " + cv_ids.Count);
			bsCommercialVehiclesCVManufacturerIDs.DataSource = cv_ids;//.Take(1000);

			IList<IDriversCab> cv_drivercabs = cv.DriversCabs;
			log("DriversCabs for current commercial vehicle : " + cv_drivercabs.Count);
			bsCommercialVehiclesDriversCabs.DataSource = cv_drivercabs;//.Take(1000);

			IList<IEngine> cv_engines = cv.Engines;
			log("Engines for current commercial vehicle : " + cv_engines.Count);
			bsCommercialVehiclesEngines.DataSource = cv_engines;//.Take(1000);

			IList<ICommercialVehicleSubType> cv_subtypes = cv.SubTypes;
			log("Subtypes for current commercial vehicle : " + cv_subtypes.Count);
			bsCommercialVehiclesSubTypes.DataSource = cv_subtypes;//.Take(1000);

		}

		private void bsAxles_PositionChanged(object sender, EventArgs e)
		{
			IAxle axle = bsAxles.Current as IAxle;
			IList<ILinkitemAttribute> axAttributes = axle.Attributes;
			log("LinkitemAttributes for current axle : " + axAttributes.Count);
			bsAxleAttributes.DataSource = axAttributes.Take(1000);

			richTextBox10.Clear();
			richTextBox10.AppendText("\nCan be displayed : " + axle.CanBeDisplayed);
			richTextBox10.AppendText("\nConstructionInterval -> AttributeGroup : " + axle.ConstructionInterval.AttributeGroup.ToString());
			richTextBox10.AppendText("\nConstructionInterval -> DisplayTitle : " + axle.ConstructionInterval.DisplayTitle);
			richTextBox10.AppendText("\nConstructionInterval -> DisplayValue : " + axle.ConstructionInterval.DisplayValue);
			richTextBox10.AppendText("\nConstructionInterval -> From : " + axle.ConstructionInterval.From);
			richTextBox10.AppendText("\nConstructionInterval -> To : " + axle.ConstructionInterval.To);
			richTextBox10.AppendText("\nDescription : " + axle.Description);
			richTextBox10.AppendText("\nFullDescription : " + axle.FullDescription);
			richTextBox10.AppendText("\nHasLink : " + axle.HasLink);
			richTextBox10.AppendText("\nID : " + axle.ID);
			richTextBox10.AppendText("\nIsAxle : " + axle.IsAxle);
			richTextBox10.AppendText("\nIsCommercialVehicle : " + axle.IsCommercialVehicle);
			richTextBox10.AppendText("\nIsCVManufacturerID : " + axle.IsCVManufacturerID);
			richTextBox10.AppendText("\nIsEngine : " + axle.IsEngine);
			richTextBox10.AppendText("\nIsMotorbike : " + axle.IsMotorbike);
			richTextBox10.AppendText("\nIsPassengerCar : " + axle.IsPassengerCar);
			richTextBox10.AppendText("\nIsTransporter : " + axle.IsTransporter);
			richTextBox10.AppendText("\nIsValidForCurrentCountry : " + axle.IsValidForCurrentCountry);
			richTextBox10.AppendText("\nLinkitemType : " + axle.LinkitemType);

			IList<IAxleModel> axModel = axle.AxleModels;
			log("AxleModels for current axle : " + axModel.Count);
			bsAxleModels.DataSource = axModel.Take(1000);
		}

		private void button7_Click(object sender, EventArgs e)
		{
			IArticle article = bsArticle.Current as IArticle;
			if (article != null)
			{
				createLinkItemTree(article);
				MessageBox.Show("Done!");
			}
		}

		private void button8_Click(object sender, EventArgs e)
		{
			FieldInfo fo = typeof(TMDVD.Logic.DVDController).GetField("bdfArticleDAL", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			IEnumerable<IArticle> en = (IEnumerable<IArticle>)fo.FieldType.GetMethod("GetAllArticles").Invoke(fo.GetValue(cnt), new object[] { bsSupplayer.Current as ISupplier });

			int iCount = int.Parse(textBox1.Text);
			IEnumerable<IArticle> en2 = en.Take(iCount);

			treeList2.Nodes.Clear();
			treeList2.BeginUpdate();
			TreeListNode rootNode = treeList2.Nodes.Add(new Object[] { "root", "" });
			showObject("Articles", en2, 0, rootNode, new Stack());
			treeList2.EndUpdate();

			MessageBox.Show("Done!");
		}

		private void button9_Click(object sender, EventArgs e)
		{
			IManufacturer man = bsAllManufacturers.Current as IManufacturer;
			int iCount = int.Parse(textBox2.Text);
			IEnumerable<IEngine> en = cnt.GetEnginesV1(man).Take(iCount);

			treeList3.Nodes.Clear();
			treeList3.BeginUpdate();
			TreeListNode rootNode = treeList3.Nodes.Add(new Object[] { "root", "" });
			showObject("Engines", en, 0, rootNode, new Stack());
			treeList3.EndUpdate();

			MessageBox.Show("Done!");			
		}

		private void button10_Click(object sender, EventArgs e)
		{
			IManufacturer man = bsAllManufacturers.Current as IManufacturer;
			int iCount = int.Parse(textBox3.Text);
			IEnumerable<IModel> model = cnt.GetModelsV1(man).Take(iCount);
			
			treeList4.Nodes.Clear();
			treeList4.BeginUpdate();
			TreeListNode rootNode = treeList4.Nodes.Add(new Object[] { "root", "" });
			showObject("Models", model, 0, rootNode, new Stack());
			treeList4.EndUpdate();

			MessageBox.Show("Done!");
		}

		private void button11_Click(object sender, EventArgs e)
		{
			IEnumerable<IMotorbike> mo = cnt.GetMotorbikesV1(bsModels.Current as IModel);

			treeList5.Nodes.Clear();
			treeList5.BeginUpdate();
			TreeListNode rootNode = treeList5.Nodes.Add(new Object[] { "root", "" });
			showObject("Motorbikes", mo, 0, rootNode, new Stack());
			treeList5.EndUpdate();

			MessageBox.Show("Done!");
		}

		private void button12_Click(object sender, EventArgs e)
		{
			IEnumerable<IPassengerCar> pc = cnt.GetPassengerCarsV1(bsModels.Current as IModel);

			treeList6.Nodes.Clear();
			treeList6.BeginUpdate();
			TreeListNode rootNode = treeList6.Nodes.Add(new Object[] { "root", "" });
			showObject("PassengerCars", pc, 0, rootNode, new Stack());
			treeList6.EndUpdate();

			MessageBox.Show("Done!");
		}

		private void button13_Click(object sender, EventArgs e)
		{
			IEnumerable<ICommercialVehicle> cv = cnt.GetCommercialVehiclesV1(bsModels.Current as IModel);

			treeList7.Nodes.Clear();
			treeList7.BeginUpdate();
			TreeListNode rootNode = treeList7.Nodes.Add(new Object[] { "root", "" });
			showObject("CommercialVehicles", cv, 0, rootNode, new Stack());
			treeList7.EndUpdate();

			MessageBox.Show("Done!");
		}

		private void button14_Click(object sender, EventArgs e)
		{
			IEnumerable<IAxle> ax = cnt.GetAxlesV1 (bsModels.Current as IModel);

			treeList8.Nodes.Clear();
			treeList8.BeginUpdate();
			TreeListNode rootNode = treeList8.Nodes.Add(new Object[] { "root", "" });
			showObject("Axles", ax, 0, rootNode, new Stack());
			treeList8.EndUpdate();
			
			MessageBox.Show("Done!");
		}

		private void testObject(Object obj)
		{
			if (obj != null)
			{
				String valueStr = obj.ToString().Replace(" ", string.Empty);
				
				if ( valueStr.Contains("2524128") || valueStr.Contains("1315070") || valueStr.Contains("665752") || 
					valueStr.Contains("330924330000") || valueStr.Contains("0330920388") || valueStr.Contains("3342164") || valueStr.Contains("815070") )
				{
					MessageBox.Show("Nasiel : " + valueStr);
				}
			}
			
		}

		private void createLinkItemReverseTree(IArticle article)
		{
			treeList9.Nodes.Clear();
			treeList9.BeginUpdate();
			TreeListNode rootNode = treeList9.Nodes.Add(new Object[] { "root", "" });
			IDictionary<ESearchTreeType, IList<IReverseLinkitem>> tmpMap = cnt.GetLinkedItemsV1(article) as IDictionary<ESearchTreeType, IList<IReverseLinkitem>>;
			showObject("Articles", tmpMap, 0, rootNode, new Stack());
			treeList9.EndUpdate();

			MessageBox.Show("Done!");
		}

		private void button15_Click(object sender, EventArgs e)
		{
			IArticle article = bsArticle.Current as IArticle;
			if (article != null)
			{
				createLinkItemReverseTree(article);
				MessageBox.Show("Done!");
			}
		}

		private void button16_Click(object sender, EventArgs e)
		{
			treeList10.BeginUpdate();
			treeList10.Nodes.Clear();
			TreeListNode rootNode = treeList10.Nodes.Add(new Object[] { "root", "" });
			IList<IDirectSearchArticle> art = cnt.SearchArticleV1("");
			showObject("test", art, 0, rootNode, new Stack());
			treeList10.EndUpdate();

			MessageBox.Show("Done!");
		}
	}
}