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

        // --- 1. OTVORENIE SÚBORU A OŠETRENIE CHÝB ---
        private void BtnOtvoriť_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "DICOM súbory (*.dcm)|*.dcm|Všetky súbory (*.*)|*.*",
                Title = "Vyberte DICOM súbor"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _aktualnySubor = DicomFile.Open(openFileDialog.FileName);
                    TxtSubor.Text = Path.GetFileName(openFileDialog.FileName);
                    BtnAnonymizovať.IsEnabled = true;

                    NaplnPrehlad();
                    NaplnTabulkuTagov();
                    SpracujObraz();
                }
                catch (Exception ex)
                {
                    VyčistiStav();
                    MessageBox.Show($"Súbor sa nepodarilo načítať. Možno nejde o validný DICOM formát.\n\nDetail: {ex.Message}",
                                    "Chyba pri načítaní", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void VyčistiStav()
        {
            _aktualnySubor = null;
            _dicomObraz = null;
            TxtSubor.Text = "Chyba pri načítaní";
            ObrazDisplej.Source = null;
            BtnAnonymizovať.IsEnabled = false;
            ZoznamTagov.Clear();
            PnlPacient.Children.Clear();
            PnlStudia.Children.Clear();
            PnlSeria.Children.Clear();
            PnlSnimka.Children.Clear();
            PnlWindowing.Visibility = Visibility.Collapsed; // Skryjeme tiahla
            TxtUpozornenie.Visibility = Visibility.Collapsed;
        }

        // --- 2. SPRACOVANIE OBRAZU A WINDOWING ---
        private void SpracujObraz()
        {
            // 1. Vyčistenie obrazovky pred načítaním nového súboru
            _dicomObraz = null;
            ObrazDisplej.Source = null;
            TxtUpozornenie.Visibility = Visibility.Collapsed;
            PnlWindowing.Visibility = Visibility.Collapsed; // Skryjeme posuvníky, kým nezistíme, či sa načítajú
            PnlPredvolby.Visibility = Visibility.Collapsed;

            if (_aktualnySubor == null) return;
            var dataset = _aktualnySubor.Dataset;

            // Súbor bez pixelov (napr. RTPLAN)
            if (!dataset.Contains(DicomTag.PixelData))
            {
                TxtUpozornenie.Text = "Súbor neobsahuje obrazové dáta (napr. RTPLAN).\nZobrazila sa iba hlavička.";
                TxtUpozornenie.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                // Inicializácia obrázka
                _dicomObraz = new DicomImage(dataset);

                // TESTOVACÍ RENDER: Skúsime, či knižnica naozaj vie rozkódovať pixely (odhalí JPEG2000)
                var testBitmap = _dicomObraz.RenderImage().AsWriteableBitmap();

                // Ak test prešiel, môžeme zobraziť obrázok a POVOLIŤ posuvníky
                ObrazDisplej.Source = testBitmap;
                PnlWindowing.Visibility = Visibility.Visible;

                string modalita = dataset.GetSingleValueOrDefault(DicomTag.Modality, "");
                if (modalita == "CT")
                    PnlPredvolby.Visibility = Visibility.Visible;

                InicializujOkno(dataset);
            }
            catch (Exception ex)
            {
                // Ak testovací render zlyhal (napr. chýba kodek pre JPEG 2000)
                _dicomObraz = null;
                ObrazDisplej.Source = null;
                PnlWindowing.Visibility = Visibility.Collapsed; // Tiahla ostanú skryté

                TxtUpozornenie.Text = "Obraz nie je možné zobraziť (komprimovaný alebo nepodporovaný formát).\nHlavička je dostupná nižšie.";
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

        // Táto metóda sa volá LEN pri posúvaní tiahel
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
                // Ak by zlyhalo prekreslenie počas posúvania
                ObrazDisplej.Source = null;
                PnlWindowing.Visibility = Visibility.Collapsed; // Schováme tiahla
                TxtUpozornenie.Text = "Chyba pri prekresľovaní obrazu.";
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

        private void BtnMäkkéTkanivá_Click(object sender, RoutedEventArgs e)
        {
            SliderWC.Value = 40;
            SliderWW.Value = 400;
        }

        private void BtnKosti_Click(object sender, RoutedEventArgs e)
        {
            SliderWC.Value = 600;
            SliderWW.Value = 1600;
        }

        // --- 3. PREHĽAD A FORMÁTOVANIE ---
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
            PridajRiadok(PnlPacient, "Dátum nar.:", UpravDatum(ds.GetSingleValueOrDefault(DicomTag.PatientBirthDate, "-")));
            PridajRiadok(PnlPacient, "Pohlavie:", ds.GetSingleValueOrDefault(DicomTag.PatientSex, "-"));

            PridajRiadok(PnlStudia, "Dátum:", UpravDatum(ds.GetSingleValueOrDefault(DicomTag.StudyDate, "-")));
            PridajRiadok(PnlStudia, "Popis:", ds.GetSingleValueOrDefault(DicomTag.StudyDescription, "-"));
            PridajRiadok(PnlStudia, "UID:", ds.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "-"));

            PridajRiadok(PnlSeria, "Modalita:", ds.GetSingleValueOrDefault(DicomTag.Modality, "-"));
            PridajRiadok(PnlSeria, "Popis:", ds.GetSingleValueOrDefault(DicomTag.SeriesDescription, "-"));
            PridajRiadok(PnlSeria, "UID:", ds.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, "-"));

            PridajRiadok(PnlSnimka, "Číslo:", ds.GetSingleValueOrDefault(DicomTag.InstanceNumber, "-"));

            string rozmery = "-";
            if (ds.Contains(DicomTag.Rows) && ds.Contains(DicomTag.Columns))
            {
                rozmery = $"{ds.GetSingleValue<ushort>(DicomTag.Rows)} x {ds.GetSingleValue<ushort>(DicomTag.Columns)}";
            }
            PridajRiadok(PnlSnimka, "Rozmery:", rozmery);
        }

        private void PridajRiadok(Panel panel, string popis, string hodnota)
        {
            TextBlock tb = new TextBlock
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

        // --- 4. TABUĽKA VŠETKÝCH TAGOV ---
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
                        hodnota = $"<binárne dáta: {de.Length} bajtov>";
                    else
                        hodnota = "<binárne dáta>";
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

        // --- 5. ANONYMIZÁCIA ---
        private void BtnAnonymizovať_Click(object sender, RoutedEventArgs e)
        {
            if (_aktualnySubor == null) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "DICOM súbor (*.dcm)|*.dcm",
                FileName = "anonymizovany_" + TxtSubor.Text,
                Title = "Uložiť anonymizovanú kópiu"
            };

            if (saveFileDialog.ShowDialog() == true)
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

                    DicomFile anonymnySubor = new DicomFile(anonymnyDataset);
                    anonymnySubor.Save(saveFileDialog.FileName);

                    MessageBox.Show("Anonymizovaná kópia bola úspešne uložená.", "Úspech", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba pri ukladaní anonymizovaného súboru:\n{ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
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