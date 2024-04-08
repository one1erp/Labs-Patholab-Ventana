using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VentanaHL7;
using Patholab_DAL_V1;
namespace Ventana_test
{
    public partial class Form1 : Form
    {
        private VentanaHL7X ventana;
        private VentanaOrderX newOrder;
        private bool _firstConnection = true;
        private int _counter = 398;
        private DataLayer dal = new DataLayer();
        private ALIQUOT aliquot;
        private DateTime timer;
        private ManualResetEvent manualResetEvent = new ManualResetEvent(false); // this waits for a signal from the machine
        public Form1()
        {
            InitializeComponent();
            string a = "xbxcxd";
            string b = "abcd";
            log(contains("aaabaa","b").ToString());

           log(IsAContainsBJumps(a,b).ToString());

            log(IsAContainsBJumps2ndAttempt(a,b).ToString());
            return;
            timer = DateTime.Now;
            log(GetBarcodeWithITF("12345"));

            ThreadStart ventanaRef = new ThreadStart(RunVentanaRun);
            Thread ventanaThread = new Thread(ventanaRef);
            ventanaThread.Start();
            log(GetBarcodeWithITF("12345"));
            if (!manualResetEvent.WaitOne(10000))
            {
                log("Print Ventana action Faild");
                return;
            }
            else
            {
                log("Print Ventana action Succeded");
            }
            log("after");


        }
        private string GetBarcodeWithITF(string barcode)
        {
            int i;
            if (!int.TryParse(barcode, out i))
            {
                return "1";
            }
            i = 0;
            int sum = 0;
            int digit;
            bool odd = true;// firs is odd
            foreach (char cdigit in barcode)
            {
                digit = int.Parse(cdigit.ToString());
                if (odd)
                {
                    sum += digit * 3;
                }
                else
                {
                    sum += digit ;
                }
                odd = !odd;
            }
            int checksum = sum % 10;
            checksum = (10 - checksum) % 10;
            return barcode + checksum.ToString();
        }
        private void RunVentanaRun()
        {
            dal.MockConnect(
                "metadata=res://*/Model1.csdl|res://*/Model1.ssdl|res://*/Model1.msl;provider=Oracle.ManagedDataAccess.Client;provider connection string=\"data source=LIMSPROD;password=lims;persist security info=True;user id=LIMS\"");
            string ventanaip = "192.168.0.38";
            int port = 58000;
            log("started");
            ventana = new VentanaHL7X();
            // ventana.PrivateFolder = @"c:\temp\";
            log("PrivateFolder:" + ventana.PrivateFolder);

            Configuration configuration = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            int.TryParse(configuration.AppSettings.Settings["Counter"].Value, out _counter);
            _counter++;

            configuration.AppSettings.Settings["Counter"].Value = _counter.ToString();
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");

            ventana.OnConnection += ventana_OnConnection;
            ventana.OnQueryOrderResult += ventana_OnQueryOrderResult;
            ventana.OnDisconnection += ventana_OnDisconnection;
            ventana.OnProtocolsResult += ventana_OnProtocolsResult;
            ventana.OnTemplatesResult += ventana_OnTemplatesResult;
            ventana.OnOrderPlaced += ventana_OnOrderPlaced;
            ventana.OnLogEvent += ventana_OnLogEvent;
            ventana.OnOrderResult += ventana_OnOrderResult;


            ventana.Open(ventanaip, port);
      
        }

        void ventana_OnOrderResult(VentanaResultX AResult)
        {
            log("%^&$%&$%&^$%");
            log(AResult.FillerSlideID);
            log("%^&$%&Order Result$%&^$%");


        }

        void ventana_OnLogEvent(string AText)
        {
           log("OnLogEvent:\r\n" + AText);
            log("!!!" + newOrder.FillerSlideID);
            //result
            //MSH|^~\&|VIP|Pathology Lab|LIS|Pathology Lab|20160709233647448||ORL^O22|VMSG2|P|2.4|
            //MSA|AA|MSG1|
            //PV1|||||||1^LIS^Nautilus^1|
            //SAC|
            //ORC|OK|TEST69/16|00073||ID|L~B~E~I|||||||||||||||P^Patho Lab|
            //OBR|1|1269|00073|22^MELAN A^STAIN||||||||||||||Patho-Lab|

            if (AText.Contains("|ORL^O22|"))
            {
                //this is a result
                //Take the OBR part
                string obr = AText.Split(new string[] { "\rOBR|" }, options: StringSplitOptions.None)[1];
                //split it. the nautilus I.D is 1, the internal ID is 2
                string[] obrArray = obr.Split(new char[] { '|' }, StringSplitOptions.None);
                if (newOrder.PlacerSlideID == obrArray[1])
                {
                    //     ventana.QueryOrder(obrArray[2],true,true);
                    //UpdateDbAndPrint();
                    aliquot.EXTERNAL_REFERENCE = obrArray[2];
                    log("Set:"+(DateTime.Now - timer).ToString());
                    
                    manualResetEvent.Set();
                }

            }
        }

        void ventana_OnOrderPlaced(bool Accepted, string Message, VentanaOrderX AnOrder)
        {
            log("OnOrderPlaced:\r\n" + Accepted.ToString() + ":" + Message + ":" + AnOrder.FillerSlideID);

        }

        void ventana_OnTemplatesResult(IVentanaCollection ATemplates)
        {
            log("===========");
            foreach (IVentanaTemplate at in ATemplates)
            {
                log("On Tamplate Result:\r\n" + at.TemplateName + ":" + at.ModifiedDate);
            }
            log("===========");
        }

        void ventana_OnProtocolsResult(IVentanaCollection AProtocols)
        {
            log("start");
            int i = 0;
            string csvProtocols = "Color Name,Protocol ID\r\n";
            foreach (IVentanaProtocol ap in AProtocols)
            {
                csvProtocols += ap.ProtocolID.Split('^')[1] + "," + ap.ProtocolID + "\r\n";
            }
            Clipboard.SetText(csvProtocols);
            log(csvProtocols);
            log("end");
        }

        void ventana_OnDisconnection()
        {
            log(ventana.Connected ? "OnConnected-disconected" : "Disconnected");
        }

        void ventana_OnQueryOrderResult(IVentanaResult AnOrder)
        {
            log("Query Order Result:\r\n" + AnOrder.FillerSlideID);
        }

        private void ventana_OnConnection()
        {
            log(ventana.Connected ? "OnConnected" : "????Disconnected");
            log( (DateTime.Now-timer).Milliseconds.ToString());
            if (_firstConnection)
            {

                _firstConnection = false;

                //ventana.QueryProtocols();
                //22^MELAN A^STAIN
                //ventana.QueryTemplates();

                aliquot = dal.FindBy<ALIQUOT>(al => al.NAME == "B000008/16.2.1.2").FirstOrDefault();
                if (aliquot != null && aliquot.ALIQUOT_USER.U_COLOR_TYPE!=null)
                {
                    U_PARTS part = dal.FindBy<U_PARTS>(p => p.U_PARTS_USER.U_STAIN == aliquot.ALIQUOT_USER.U_COLOR_TYPE
                                                            && p.U_PARTS_USER.U_PART_TYPE == "I").FirstOrDefault();
                    if (part != null)
                    {
                        //                                          part.DESCRIPTION is the ventana staining code
                        newOrder = ventana.NewOrder(aliquot.ALIQUOT_ID.ToString(), part.DESCRIPTION, "Patho-Lab", true, true,
                                                    true, true);

                        //   newOrder = ventana.NewOrder("12" + _counter.ToString(), "22^MELAN A^STAIN", "Patho-Lab", true, true, true, true);
                        //  newOrder = ventana.NewOrder("12" + _counter.ToString(), "22^MELAN A^STAIN", "", false, false, true, false);

                        //Case ID
                        newOrder.SetFieldValue("ORC.2", aliquot.NAME);

                        newOrder.SetFieldValue("ORC.21", "P^Patho Lab");
                        //Requester
                        newOrder.SetFieldValue("PV1.7", "1^LIS^Nautilus^1");

                        newOrder.SetFieldValue("OBR.1", "1");

                        // log("\n" + newOrder.ToString() + "\n");
                        // ventana.InitiateAction();
                        log("!!!@!@!@" + newOrder.PlaceOrder().ToString());

                        //wait for mre.set()
                      
                        // ventana.Close();
                        //ventana.InitiateAction();
                        // ventana.DoubleBuffered = true;
                        //log("DoubleBuffered:"+ventana.DoubleBuffered.ToString());

                        //  log("!!!" + newOrder.FillerSlideID);
                        //ventana.QueryOrder("25",true,true);
                        //   log(newOrder.GetFieldValue("Requester"));
                    }
                }
            }


        }

        private void log(string text)
        {
            textBox1.Text = (DateTime.Now-timer).ToString() + ":" + text + "\r\n" + textBox1.Text;
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                ventana.Close();
            }
            catch (Exception)
            {
               
            }
            
        }

        private bool IsAContainsBJumps2ndAttempt(string a, string b)
        {

            int aIndex;
            //'aIndex'  points to the zero base index of the comparison on 'a' string
          

            bool allLettersAreEqual ;

            //Go throw avalable aIndex locations   
            //The max value is length(a) - 2 * length (b)  + 1
            //calculated from the comparison line 
            //"  if (a[aIndex + 2 * bIndex] != b[bIndex]) " 
            //with bIndex max Value put in the a[aIndex +2 * bIndex]
            //aIndex +(b.length-1)*2<=a.length-1
            for (aIndex = 0; aIndex <= a.Length - 2* b.Length + 1; aIndex++)
            {
                //Assume all letters are equal
                allLettersAreEqual = true;
                for (int bIndex = 0; bIndex <= b.Length - 1; bIndex++)
                {
                    //Check if any of the letters are different. Continue if a different letter is found (allLettersAreEqual==false)
                    //Use the b(bIndex)-> a(2bIndex+aIndex) translation
                    if (allLettersAreEqual && a[aIndex + 2 * bIndex] != b[bIndex])
                    {
                        allLettersAreEqual = false;
                    }
                }
                //At the end of each comparison, if the comparison value is true, return true, if not, continue to the next aIndex
                if (allLettersAreEqual) return true;
            }


            return false;
        }

        bool IsAContainsBJumps(string a, string b)
        {
            bool oddLocation=true;
            string lettersOfAInOddocation="";
            string lettersOfAInEvenLocation="";

            //split a in two, even and odd part 
            foreach (char letter in a)
            {
                if (oddLocation)
                    lettersOfAInOddocation += letter;
                else
                    lettersOfAInEvenLocation += letter;
                oddLocation = !oddLocation;
            }
             //return true if the odd or even letters contains b
            return contains(lettersOfAInOddocation,b) || contains (lettersOfAInEvenLocation,b);
        }
        bool contains(string a, string b)
        {
           
            if (a == b) return true;
            if (a == "") return false;
            string rightA = ""; // The letters on the left without the final letter
            string leftA = "";// The letters on the right without the final letter
            for (int i = 0; i <= a.Length-2; i++)
            {
                rightA += a[i];
                leftA += a[i + 1];
            }
            return contains(rightA, b) || contains(leftA, b); // check if the substrings of  a  are equal to b
        }
         bool recorsiveIsAContainsBJumps(string a, string b)
         {
             //this solution is more complicated and will work by the following priciple - 
             // if 'a' == 'b' and the length is 1 , return true 
             // else 
             // if the first letter of 'a' and 'b' is the same 
             //->check if the substring that is of double the size of 'b' -1  and the first letter of 'b' and return the comparison
             //-> also remove the 
             // here is an illustration of the flow
             //  xxkkxsxxxx,ks
             //  xkkxsxxxx,ks
             //  kkxsxxxx,ks
             //  kkxsxxxx,ks
             // xs,s  kxsxxxx,ks
             //

             if (a == b && b.Length <= 1)
                 return true;
             if (a.Length <= 1 || b.Length <= 1 || a.Length < (b.Length * 2 - 1))
                     return false;
                  if (a.First() == b.First())
                     return (recorsiveIsAContainsBJumps(a.Substring(2, b.Length*2 - 3), b.Substring(1)) ||
                             recorsiveIsAContainsBJumps(a.Substring(1), b));
             return recorsiveIsAContainsBJumps(a.Substring(1), b);
              
         }
        private void CheckConnection_Click(object sender, EventArgs e)
        {
            log(ventana.Connected ? "Connected" : "Disconnected");
        }
    }
}
