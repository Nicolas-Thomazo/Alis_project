using System;
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

//using System.Speech.Synthesis;
//using SpeechSynthesizer = Microsoft.CognitiveServices.Speech.SpeechSynthesizer;

namespace Demoo.Views
{

    public sealed partial class MainPage : Page
    {


        public VoiceInformation SelectedVoice { get; set; }
        public string TextInput { get; set; }
        

        private ObservableCollection<String> finDePhrase = new ObservableCollection<String>();
        public ObservableCollection<String> FinDePhrase { get { return this.finDePhrase; } }

        private ObservableCollection<String> topPhrases = new ObservableCollection<String>();
        public ObservableCollection<String> TopPhrases { get { return this.topPhrases; } }

        private ObservableCollection<String> questionsReponses = new ObservableCollection<String>();
        public ObservableCollection<String> QuestionsReponses { get { return this.questionsReponses; } }

        string url_questions_reponses = "http://127.0.0.1:5000/questions_reponses";
        string url_insert_to_databases = "http://127.0.0.1:5000/insert_to_dabatase";
        string url_fin_phrases = "http://127.0.0.1:5000/generate_sentences_english_gpt2";

        string CONFIG_LOCATIOn = "francecentral";
        string CONFIG_KEY = "1c514fb1fea2465a882f0fae12242cc0";
        string CONFIG_LAN = "fr-FR";

        bool CurrentlyAddingToListView = false;

        private Stream stream = null;
        public MainPage()
        {
            this.InitializeComponent();
            this.TopPhrases.Add("J'ai envie d'aller me balader.");
            this.TopPhrases.Add("Tu peux appeler le médecin ?.");
            this.TopPhrases.Add("Quel temps fait-il demain ?");
            this.TopPhrases.Add("Qu'est-ce qu'on mange aujourd'hui ?");

        }




        /// <summary>
        /// Permet de faire un appel à l'api flask python
        /// </summary>
        /// <param name="id">Id de l'interlocuteur qui parle qu'on envoie au modèle pour l'instant constante car fonctionnalité non implémenté</param>
        /// <param name="phrase">Phrase ou début de phrase qu'on va envoyer au modèle</param>
        /// <returns></returns>
        private async Task<String> TryPostJsonAsync(string id, string phrase, string url)
        {
            string  exceptionMessage= string.Empty;
            string webResponse =  " ";
            try
            {
                Uri uri = new Uri(url);
                WebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream(), Encoding.UTF8))
                {
                    dynamic jsonReponse = new JObject();
                    jsonReponse.id = id;
                    jsonReponse.phrase = phrase;
                    streamWriter.Write(jsonReponse.ToString());
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
                //MessageDialog dlg = new MessageDialog(exceptionMessage);
                //await dlg.ShowAsync();
                //return webResponse;
            }

            return webResponse;
           
        }
        /// <summary>
        /// Permet de sauvegarder le contenu sous forme de fichier audio
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string exceptionMessage = string.Empty;
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
                exceptionMessage = $"An error occurred. {exception.Message}";
                MessageDialog dlg = new MessageDialog(exceptionMessage);
                await dlg.ShowAsync();
            }
        }
        /// <summary>
        /// Quand l'utilisateur click sur un element de la liste des propositions de réponses, le oontenu va être dit à voix haute
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            string exceptionMessage = string.Empty;
            try
            {
                DeleteFinDePhrase();
                GetTextCallApi(TextInput, url_fin_phrases, 0);
            }
            catch (Exception exception)
            {
                exceptionMessage = $"An error occurred. {exception.Message}";
                MessageDialog dlg = new MessageDialog(exceptionMessage);
                await dlg.ShowAsync();
            }
        }
        private async void SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            await SynthesizeAudioAsync(TextInput);
        }
        /// <summary>
        /// Quand l'utilisateur click sur le boutton speech to text, on récupère ce qui est dit à l'oral et on l'envoie à l'api
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SpeechRecognitionFromMicrophone_ButtonClicked(object sender, RoutedEventArgs e)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription(CONFIG_KEY, CONFIG_LOCATIOn);
            config.SpeechRecognitionLanguage = CONFIG_LAN;

            try
            {
                


                // Creates a speech recognizer using microphone as audio input.
                using (var recognizer = new SpeechRecognizer(config))
                {

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


                    //ON SUPPRIME LES ELEMENTS DE LA LISTE SI Y EN A
                    DeleteQuestionsReponses();

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
        /// <summary>
        /// Permet de faire l'appel à la fonction TryPostJsonAsync qui fait envoie le texte à l'api. Avec le texct recupéré, on appelle les fonctions qui permettent de l'ajouter à l'interface.
        /// </summary>
        /// <param name="texte">Texte qui est envoyé à l'api sur lequel on va faire la précition</param>
        /// <param name="url">url de l'api en question</param>
        /// <param name="choice">entier, 0 ou 1 en fonction de si on veut prédire une fin de phrase ouune réponse à une question</param>
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
        /// <summary>
        /// On recupère les réponses, on utilise du regex pour cleanles phrases et ensuite on le rajoute à notre listView
        /// </summary>
        /// <param name="reponse_string"></param>
        private async void AddPredictionToFinDePhrase(string reponse_string)
        {
            String split = "\",";

            string[] listeReponseModele = reponse_string.Split(split);
            CurrentlyAddingToListView = true;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                foreach (String phrase in listeReponseModele)
                {
                    
                    string temp = Regex.Replace(phrase, @"\|]|/|\n|\[|\r|]", "");
                    string temp2 = Regex.Replace(temp, "\"", "");
                    this.FinDePhrase.Add(temp2);
                    
                }

            });
            CurrentlyAddingToListView = false;
        }
        /// <summary>
        /// On recupère les réponses, on utilise du regex pour cleanles phrases et ensuite on le rajoute à notre listView
        /// </summary>
        /// <param name="reponse_string"></param>
        private async void AddPredictionToQuestionsReponses(string reponse_string)
        {
            String split = "\",";

            string[] listeReponseModele = reponse_string.Split(split);
            CurrentlyAddingToListView = true;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                foreach (String phrase in listeReponseModele)
                {
                    string temp = Regex.Replace(phrase, @"\|]|/|\n|\[|\r|]", "");
                    string temp2 = Regex.Replace(temp, "\"", "");
                    this.QuestionsReponses.Add(temp2);
                }

            });
            CurrentlyAddingToListView = false;
        }
        /// <summary>
        /// On supprime les anciennes prédictions pour pouvoir montrer les nouvelles
        /// </summary>
        private async void DeleteFinDePhrase()
        {
            CurrentlyAddingToListView = true;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                foreach (var x in FinDePhrase.ToList())
                {
                    FinDePhrase.Remove(x);
                }

            });
            CurrentlyAddingToListView = false;
        }
        /// <summary>
        /// On supprime les anciennes prédictions pour pouvoir montrer les nouvelles
        /// </summary>
        private async void DeleteQuestionsReponses()
        {
            CurrentlyAddingToListView = true;
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                foreach (var x in QuestionsReponses.ToList())
                {
                    QuestionsReponses.Remove(x);
                }

            });
            CurrentlyAddingToListView = false;
        }
        /// <summary>
        /// Cette méthode est appellée quand on click sur la liste view, elle recupère l'élement selectionné et appelle ensuite la méthode SynthesizeAudioAsync.
        /// Le "if" sert à ne pas clacker sur l'item quand on supprime la liste et qu'on met de nouvelles prédictions. Si on ne le fait pas on a une erreur car il essaie de sélectionner l'élemet clické
        /// alors que la liste est null
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void FinDePhrases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CurrentlyAddingToListView == false)
            {
                string val = (sender as ListView).SelectedItem.ToString();

                await SynthesizeAudioAsync(val);
            }
        }
        /// <summary>
        /// Cette méthode est appellée quand on click sur la liste view, elle recupère l'élement selectionné et appelle ensuite la méthode SynthesizeAudioAsync.
        /// Le "if" sert à ne pas clacker sur l'item quand on supprime la liste et qu'on met de nouvelles prédictions. Si on ne le fait pas on a une erreur car il essaie de sélectionner l'élemet clické
        /// alors que la liste est null        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void QuestionsReponses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentlyAddingToListView == false)
            {
                string val = (sender as ListView).SelectedItem.ToString();

                await SynthesizeAudioAsync(val);
            }
        }
        private async void TopPhrases_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentlyAddingToListView == false)
            {
                string val = (sender as ListView).SelectedItem.ToString();

                await SynthesizeAudioAsync(val);
            }
        }


        private async Task SynthesizeAudioAsync(string input)
        {

            try
            {
                using (var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer())
                {
                    synthesizer.Voice = SelectedVoice ?? Windows.Media.SpeechSynthesis.SpeechSynthesizer.DefaultVoice;

                    var synStream = await synthesizer.SynthesizeTextToStreamAsync(input);


                    mPlayerElement.Source = MediaSource.CreateFromStream(synStream, synStream.ContentType);

                    stream = synStream.AsStream();
                    stream.Position = 0;

                }
            }
            catch (Exception exception)
            {
                var dlg = new MessageDialog(exception.Message, Package.Current.DisplayName);
                var cmd = await dlg.ShowAsync();
            }
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
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Transparent);
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

    }

}
