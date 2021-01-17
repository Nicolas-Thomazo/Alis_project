﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Demoo.ViewModels;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using System.Text.RegularExpressions;

namespace Demoo.Views
{

    public sealed partial class MainPage : Page
    {


        public VoiceInformation SelectedVoice { get; set; }
        public string TextInput { get; set; }
        private Stream stream = null;

        private ObservableCollection<String> finDePhrase = new ObservableCollection<String>();
        public ObservableCollection<String> FinDePhrase { get { return this.finDePhrase; } }

        private ObservableCollection<String> questionsReponses = new ObservableCollection<String>();
        public ObservableCollection<String> QuestionsReponses { get { return this.questionsReponses; } }

        string url_questions_reponses = "http://127.0.0.1:5000/questions_reponses";
        string url_insert_to_databases = "http://127.0.0.1:5000/insert_to_dabatase";
        string url_fin_phrases = "http://127.0.0.1:5000/debut_phrase_response2";


        public MainPage()
        {
            this.InitializeComponent();
        }



        public class Question
        {
            public string id { get; set; }
            public string texte { get; set; }

        }
        

        /// <summary>
        /// Permet de faire un appel à l'api flask python
        /// </summary>
        /// <param name="id">Id de l'interlocuteur qui parle qu'on envoie au modèle pour l'instant constante car fonctionnalité non implémenté</param>
        /// <param name="phrase">Phrase ou début de phrase qu'on va envoyer au modèle</param>
        /// <returns></returns>
        private async Task<String> TryPostJsonAsync(string id, string phrase, string url)
        {
            string uirWebAPI, exceptionMessage;
            exceptionMessage = string.Empty;
            string webResponse = string.Empty;
            try
            {
                Uri uri = new Uri(url);
                WebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream(), Encoding.UTF8))
                {
                    // Build employee test JSON objec
                    dynamic employee = new JObject();
                    employee.id = id;
                    employee.phrase = phrase;
                    streamWriter.Write(employee.ToString());
                }
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8))
                {
                    webResponse = streamReader.ReadToEnd();
                }
                
            }
            catch (Exception ex)
            {
                exceptionMessage = $"An error occurred. {ex.Message}";
            }

            return webResponse;
           
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (stream != null)
                {
                    var filePicker = new FileSavePicker()
                    {
                        CommitButtonText = "Save",
                    };
                    filePicker.FileTypeChoices.Add("wav", new List<string>() { ".wav" });
                    var file = await filePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        var _stream = await file.OpenStreamForWriteAsync();
                        await stream.CopyToAsync(_stream);
                        await _stream.FlushAsync();
                        var dlg = new MessageDialog("File saved.", Package.Current.DisplayName);
                        var cmd = await dlg.ShowAsync();
                    }
                }
            }
            catch (Exception exception)
            {
                var dlg = new MessageDialog(exception.Message, Package.Current.DisplayName);
                var cmd = await dlg.ShowAsync();
            }
        }

        private async void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DeleteFinDePhrase();
                GetTextCallApi(TextInput, url_fin_phrases, 0);

                using (var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer())
                {
                    synthesizer.Voice = SelectedVoice ?? Windows.Media.SpeechSynthesis.SpeechSynthesizer.DefaultVoice;

                    var synStream = await synthesizer.SynthesizeTextToStreamAsync(TextInput);


                    stream = synStream.AsStream();
                    stream.Position = 0;

                    //var dlg = new MessageDialog("Conversion succeeded.", Package.Current.DisplayName);
                    //var cmd = await dlg.ShowAsync();
                }
            }
            catch (Exception exception)
            {
                var dlg = new MessageDialog(exception.Message, Package.Current.DisplayName);
                var cmd = await dlg.ShowAsync();
            }
        }

        private async void SpeechRecognitionFromMicrophone_ButtonClicked(object sender, RoutedEventArgs e)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("1c514fb1fea2465a882f0fae12242cc0", "francecentral");
            config.SpeechRecognitionLanguage = "fr-FR";

            try
            {
                //ON SUPPRIME LES ELEMENTS DE LA LISTE SI Y EN A
                DeleteQuestionsReponses();


                // Creates a speech recognizer using microphone as audio input.
                using (var recognizer = new SpeechRecognizer(config))
                {
                    // Starts speech recognition, and returns after a single utterance is recognized. The end of a
                    // single utterance is determined by listening for silence at the end or until a maximum of 15
                    // seconds of audio is processed.  The task returns the recognition text as result.
                    // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
                    // shot recognition like command or query.
                    // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
                    var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                    // Checks result.
                    StringBuilder sb = new StringBuilder();
                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                        sb.AppendLine($"{result.Text}");
                    }
                    else if (result.Reason == ResultReason.NoMatch)
                    {
                        sb.AppendLine($"NOMATCH: Speech could not be recognized.");
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = CancellationDetails.FromResult(result);
                        sb.AppendLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                            sb.AppendLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            sb.AppendLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                            sb.AppendLine($"CANCELED: Did you update the subscription info?");
                        }
                    }

                    // Update the UI
                    NotifyUser(sb.ToString(), NotifyType.StatusMessage);

                    //Création de l'objet à séréaliser
                    string texte = sb.ToString();
                    Question question = new Question()
                    {
                        id = "1234",
                        texte = sb.ToString(),
                    };
                    //Séréalisation de l'objet
                    string notjsonyet = "'id':'123','debut_phrase':'" + texte+"'";
                    string jsonSerializedObj = JsonConvert.SerializeObject(question);

                    //Choix du local folder comme emplacement
                    //C:\Users\pauwe\AppData\Local\Packages\b6d47962-7028-4697-b1c3-c39fb3a25c97_npata57gt8602\LocalState\
                    //C:\Users\leays\AppData\Local\Packages\243E158E-1F25-44EF-90FC-68A410B06C6D_kqh4kznn2n4py\LocalState\
                    Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

                    //Création du fichier json contenant l'id de l'interlocuteur et son texte
                    Windows.Storage.StorageFile sampleFile = await localFolder.CreateFileAsync("dataFile.json",
                     Windows.Storage.CreationCollisionOption.ReplaceExisting);

                    //Ecriture dans le fichier json
                    await Windows.Storage.FileIO.WriteTextAsync(sampleFile, jsonSerializedObj);
                    //Envoi de la requête

                    //cal api
                    GetTextCallApi(texte, url_questions_reponses, 1);

                }
            }
            catch (Exception ex)
            {
                NotifyUser($"Enable Microphone First.\n {ex.ToString()}", NotifyType.ErrorMessage);
            }
        }
        private async void GetTextCallApi(string texte,string url,int choice)
        {
            Task<string> reponse = TryPostJsonAsync("id1", texte,url);
            string reponse_string = await reponse;
            if (choice == 0)
            {
                AddPredictionToFinDePhrase(reponse_string);
            }
            else
            {
                AddPredictionToQuestionsReponses(reponse_string);
            }
        }

        private async void AddPredictionToFinDePhrase(string reponse_string)
        {
            String split = "\",";

            string[] listeReponseModele = reponse_string.Split(split);

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                foreach (String phrase in listeReponseModele)
                {
                    string temp = Regex.Replace(phrase, @"\|]|/|\n|\[|\r|]", "");
                    string temp2 = Regex.Replace(temp, "\"", "");
                    this.FinDePhrase.Add(temp2);
                }

            });
        }
        private async void AddPredictionToQuestionsReponses(string reponse_string)
        {
            String split = "\",";

            string[] listeReponseModele = reponse_string.Split(split);

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                foreach (String phrase in listeReponseModele)
                {
                    string temp = Regex.Replace(phrase, @"\|]|/|\n|\[|\r|]", "");
                    string temp2 = Regex.Replace(temp, "\"", "");
                    this.QuestionsReponses.Add(temp2);
                }

            });
        }
        /// <summary>
        /// Delete the observableCollection because we have a new prediction
        /// </summary>
        private async void DeleteFinDePhrase()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                foreach (var x in FinDePhrase.ToList())
                {
                    FinDePhrase.Remove(x);
                }

            });
        }
        private async void DeleteQuestionsReponses()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                foreach (var x in QuestionsReponses.ToList())
                {
                    QuestionsReponses.Remove(x);
                }

            });
        }


        private enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        public static async Task SpeakerVerify(SpeechConfig config, VoiceProfile profile, Dictionary<string, string> profileMapping)
        {
            var speakerRecognizer = new SpeakerRecognizer(config, AudioConfig.FromDefaultMicrophoneInput());
            var model = SpeakerVerificationModel.FromProfile(profile);

            Console.WriteLine("Speak the passphrase to verify: \"My voice is my passport, please verify me.\"");
            var result = await speakerRecognizer.RecognizeOnceAsync(model);
            Console.WriteLine($"Verified voice profile for speaker {profileMapping[result.ProfileId]}, score is {result.Score}");
        }

        private async Task VerificationEnroll(SpeechConfig config, Dictionary<string, string> profileMapping)
        {

            using (var client = new VoiceProfileClient(config))
            using (var profile = await client.CreateProfileAsync(VoiceProfileType.TextDependentVerification, "en-us"))
            {
                using (var audioInput = AudioConfig.FromDefaultMicrophoneInput())
                {
                    Console.WriteLine($"Enrolling profile id {profile.Id}.");
                    // give the profile a human-readable display name
                    profileMapping.Add(profile.Id, "Your Name");

                    VoiceProfileEnrollmentResult result = null;
                    while (result is null || result.RemainingEnrollmentsCount > 0)
                    {
                        Console.WriteLine("Speak the passphrase, \"My voice is my passport, verify me.\"");
                        result = await client.EnrollProfileAsync(profile, audioInput);
                        Console.WriteLine($"Remaining enrollments needed: {result.RemainingEnrollmentsCount}");
                        Console.WriteLine("");
                    }

                    if (result.Reason == ResultReason.EnrolledVoiceProfile)
                    {
                        await SpeakerVerify(config, profile, profileMapping);
                    }
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = VoiceProfileEnrollmentCancellationDetails.FromResult(result);
                        Console.WriteLine($"CANCELED {profile.Id}: ErrorCode={cancellation.ErrorCode} ErrorDetails={cancellation.ErrorDetails}");
                    }
                }
            }
        }

        private async void VoiceRecognition(object sender, RoutedEventArgs e)
        {

            string subscriptionKey = "c06aeb4d93b24003a125aa2adef59aaa";
            string region = "francecentral";
            var config = SpeechConfig.FromSubscription(subscriptionKey, region);

            // persist profileMapping if you want to store a record of who the profile is
            var profileMapping = new Dictionary<string, string>();
            await VerificationEnroll(config, profileMapping);

            Console.ReadLine();
        }

        private void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
            }
        }

        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text += string.IsNullOrEmpty(StatusBlock.Text) ? strMessage : "\n" + strMessage;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = !string.IsNullOrEmpty(StatusBlock.Text) ? Visibility.Visible : Visibility.Collapsed;
            if (!string.IsNullOrEmpty(StatusBlock.Text))
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
            // Raise an event if necessary to enable a screen reader to announce the status update.
            var peer = Windows.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(StatusBlock);
            if (peer != null)
            {
                peer.RaiseAutomationEvent(Windows.UI.Xaml.Automation.Peers.AutomationEvents.LiveRegionChanged);
            }
        }

        private void FinDePhrases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
         
            MessageDialog dlg = new MessageDialog(sender.ToString());
            dlg.ShowAsync();
        }
        private void QuestionsReponses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            MessageDialog dlg = new MessageDialog(sender.ToString());
            dlg.ShowAsync();
        }

        private void TitleTextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {


        }
    }

}