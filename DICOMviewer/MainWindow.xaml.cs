using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using Microsoft.Win32;

namespace DICOMviewer
{
    public partial class MainWindow : Window
    {
        private DicomFile _aktualnySubor;
        private DicomImage _dicomObraz;

        public ObservableCollection<TagInfo> ZoznamTagov { get; set; } = new ObservableCollection<TagInfo>();

        public MainWindow()
        {
            InitializeComponent();

            new DicomSetupBuilder()
                .RegisterServices(s => s.AddImageManager<WPFImageManager>())
                .Build();

            TagDataGrid.ItemsSource = ZoznamTagov;
        }

        private void BtnOtvorit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "DICOM subory (*.dcm)|*.dcm|Vsetky subory (*.*)|*.*",
                Title = "Vyberte DICOM subor"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _aktualnySubor = DicomFile.Open(dialog.FileName);
                    TxtSubor.Text = Path.GetFileName(dialog.FileName);
                    BtnAnonymizovat.IsEnabled = true;

                    NaplnPrehlad();
                    NaplnTabulkuTagov();
                    SpracujObraz();
                }
                catch (Exception ex)
                {
                    VycistiStav();
                    MessageBox.Show($"Subor sa nepodarilo nacitat. Mozno nejde o validny DICOM format.\n\nDetail: {ex.Message}",
                                    "Chyba pri nacitani", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void VycistiStav()
        {
            _aktualnySubor = null;
            _dicomObraz = null;
            TxtSubor.Text = "Chyba pri nacitani";
            ObrazDisplej.Source = null;
            BtnAnonymizovat.IsEnabled = false;
            ZoznamTagov.Clear();
            PnlPacient.Children.Clear();
            PnlStudia.Children.Clear();
            PnlSeria.Children.Clear();
            PnlSnimka.Children.Clear();
            PnlWindowing.Visibility = Visibility.Collapsed;
            TxtUpozornenie.Visibility = Visibility.Collapsed;
        }

        private void SpracujObraz()
        {
            _dicomObraz = null;
            ObrazDisplej.Source = null;
            TxtUpozornenie.Visibility = Visibility.Collapsed;
            PnlWindowing.Visibility = Visibility.Collapsed;
            PnlPredvolby.Visibility = Visibility.Collapsed;

            if (_aktualnySubor == null) return;
            var dataset = _aktualnySubor.Dataset;

            if (!dataset.Contains(DicomTag.PixelData))
            {
                TxtUpozornenie.Text = "Subor neobsahuje obrazove data (napr. RTPLAN).\nZobrazila sa iba hlavicka.";
                TxtUpozornenie.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                _dicomObraz = new DicomImage(dataset);
                var testBitmap = _dicomObraz.RenderImage().AsWriteableBitmap();

                ObrazDisplej.Source = testBitmap;
                PnlWindowing.Visibility = Visibility.Visible;

                string modalita = dataset.GetSingleValueOrDefault(DicomTag.Modality, "");
                if (modalita == "CT")
                {
                    PnlPredvolby.Visibility = Visibility.Visible;
                }

                InicializujOkno(dataset);
            }
            catch (Exception)
            {
                _dicomObraz = null;
                ObrazDisplej.Source = null;
                PnlWindowing.Visibility = Visibility.Collapsed;

                TxtUpozornenie.Text = "Obraz nie je mozne zobrazit (komprimovany alebo nepodporovany format).\nHlavicka je dostupna nizsie.";
                TxtUpozornenie.Visibility = Visibility.Visible;
            }
        }

        private void InicializujOkno(DicomDataset dataset)
        {
            double wc = 40;
            double ww = 400;

            try
            {
                var options = GrayscaleRenderOptions.FromDataset(dataset, 0);
                wc = options.WindowCenter;
                ww = options.WindowWidth;
            }
            catch
            {
                if (dataset.Contains(DicomTag.WindowCenter) && dataset.Contains(DicomTag.WindowWidth))
                {
                    wc = dataset.GetSingleValue<double>(DicomTag.WindowCenter);
                    ww = dataset.GetSingleValue<double>(DicomTag.WindowWidth);
                }
            }

            if (ww <= 0) ww = 1;

            SliderWC.Value = Math.Round(wc);
            SliderWW.Value = Math.Round(ww);
            TxtWC.Text = Math.Round(wc).ToString();
            TxtWW.Text = Math.Round(ww).ToString();

            VykresliObraz(wc, ww);
        }

        private void VykresliObraz(double wc, double ww)
        {
            if (_dicomObraz == null) return;

            try
            {
                _dicomObraz.WindowCenter = wc;
                _dicomObraz.WindowWidth = ww;
                ObrazDisplej.Source = _dicomObraz.RenderImage().AsWriteableBitmap();
            }
            catch
            {
                ObrazDisplej.Source = null;
                PnlWindowing.Visibility = Visibility.Collapsed;
                TxtUpozornenie.Text = "Chyba pri prekreslovani obrazu.";
                TxtUpozornenie.Visibility = Visibility.Visible;
            }
        }

        private void SliderOkno_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtWC == null || TxtWW == null) return;

            TxtWC.Text = Math.Round(SliderWC.Value).ToString();
            TxtWW.Text = Math.Round(SliderWW.Value).ToString();

            VykresliObraz(SliderWC.Value, SliderWW.Value);
        }

        private void BtnMakkeTkaniva_Click(object sender, RoutedEventArgs e)
        {
            SliderWC.Value = 40;
            SliderWW.Value = 400;
        }

        private void BtnKosti_Click(object sender, RoutedEventArgs e)
        {
            SliderWC.Value = 600;
            SliderWW.Value = 1600;
        }

        private void NaplnPrehlad()
        {
            if (_aktualnySubor == null) return;
            var ds = _aktualnySubor.Dataset;

            PnlPacient.Children.Clear();
            PnlStudia.Children.Clear();
            PnlSeria.Children.Clear();
            PnlSnimka.Children.Clear();

            PridajRiadok(PnlPacient, "Meno:", UpravMeno(ds.GetSingleValueOrDefault(DicomTag.PatientName, "")));
            PridajRiadok(PnlPacient, "ID:", ds.GetSingleValueOrDefault(DicomTag.PatientID, "-"));
            PridajRiadok(PnlPacient, "Datum nar.:", UpravDatum(ds.GetSingleValueOrDefault(DicomTag.PatientBirthDate, "-")));
            PridajRiadok(PnlPacient, "Pohlavie:", ds.GetSingleValueOrDefault(DicomTag.PatientSex, "-"));

            PridajRiadok(PnlStudia, "Datum:", UpravDatum(ds.GetSingleValueOrDefault(DicomTag.StudyDate, "-")));
            PridajRiadok(PnlStudia, "Popis:", ds.GetSingleValueOrDefault(DicomTag.StudyDescription, "-"));
            PridajRiadok(PnlStudia, "UID:", ds.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "-"));

            PridajRiadok(PnlSeria, "Modalita:", ds.GetSingleValueOrDefault(DicomTag.Modality, "-"));
            PridajRiadok(PnlSeria, "Popis:", ds.GetSingleValueOrDefault(DicomTag.SeriesDescription, "-"));
            PridajRiadok(PnlSeria, "UID:", ds.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, "-"));

            PridajRiadok(PnlSnimka, "Cislo:", ds.GetSingleValueOrDefault(DicomTag.InstanceNumber, "-"));

            string rozmery = "-";
            if (ds.Contains(DicomTag.Rows) && ds.Contains(DicomTag.Columns))
            {
                rozmery = $"{ds.GetSingleValue<ushort>(DicomTag.Rows)} x {ds.GetSingleValue<ushort>(DicomTag.Columns)}";
            }
            PridajRiadok(PnlSnimka, "Rozmery:", rozmery);
        }

        private void PridajRiadok(Panel panel, string popis, string hodnota)
        {
            var tb = new TextBlock
            {
                Text = $"{popis} {hodnota}",
                Margin = new Thickness(0, 2, 0, 2),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(tb);
        }

        private string UpravMeno(string meno)
        {
            if (string.IsNullOrWhiteSpace(meno)) return "-";
            return meno.Replace("^", " ").Trim();
        }

        private string UpravDatum(string datum)
        {
            if (string.IsNullOrWhiteSpace(datum) || datum.Length != 8) return "-";
            return $"{datum.Substring(6, 2)}.{datum.Substring(4, 2)}.{datum.Substring(0, 4)}";
        }

        private void NaplnTabulkuTagov()
        {
            ZoznamTagov.Clear();
            if (_aktualnySubor == null) return;

            foreach (var element in _aktualnySubor.Dataset)
            {
                string hodnota = "";

                if (element.Tag == DicomTag.PixelData)
                {
                    if (element is DicomElement de)
                        hodnota = $"<binarne data: {de.Length} bajtov>";
                    else
                        hodnota = "<binarne data>";
                }
                else if (element is DicomElement dicomElement && dicomElement.Count > 0)
                {
                    hodnota = dicomElement.Get<string>() ?? "-";
                }
                else
                {
                    hodnota = element.ToString() ?? "-";
                }

                ZoznamTagov.Add(new TagInfo
                {
                    Tag = $"({element.Tag.Group:X4},{element.Tag.Element:X4})",
                    VR = element.ValueRepresentation.Code,
                    Nazov = element.Tag.DictionaryEntry.Name,
                    Hodnota = hodnota
                });
            }
        }

        private void BtnAnonymizovat_Click(object sender, RoutedEventArgs e)
        {
            if (_aktualnySubor == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "DICOM subor (*.dcm)|*.dcm",
                FileName = "anonymizovany_" + TxtSubor.Text,
                Title = "Ulozit anonymizovanu kopiu"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    DicomDataset anonymnyDataset = _aktualnySubor.Dataset.Clone();

                    anonymnyDataset.AddOrUpdate(DicomTag.PatientName, "ANONYM");
                    anonymnyDataset.AddOrUpdate(DicomTag.PatientID, "999999");

                    anonymnyDataset.Remove(DicomTag.PatientBirthDate);
                    anonymnyDataset.Remove(DicomTag.InstitutionName);
                    anonymnyDataset.Remove(DicomTag.ReferringPhysicianName);

                    anonymnyDataset.AddOrUpdate(DicomTag.PatientIdentityRemoved, "YES");

                    var anonymnySubor = new DicomFile(anonymnyDataset);
                    anonymnySubor.Save(dialog.FileName);

                    MessageBox.Show("Anonymizovana kopia bola uspesne ulozena.", "Uspech", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba pri ukladani anonymizovaneho suboru:\n{ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class TagInfo
    {
        public string Tag { get; set; }
        public string VR { get; set; }
        public string Nazov { get; set; }
        public string Hodnota { get; set; }
    }
}