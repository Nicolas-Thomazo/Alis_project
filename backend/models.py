from transformers import pipeline
import tensorflow as tf
import transformers
import numpy as np
#from googletrans import Translator
import pandas as pd 
import re
from google_trans_new import google_translator  

max_length = 32  # Maximum length of input sentence to the model.
batch_size = 32
epochs = 2
# Labels in our dataset.
labels = ["contradiction", "entailment", "neutral"] 




class BertSemanticDataGenerator(tf.keras.utils.Sequence):
    """Generates batches of data.

    Args:
        sentence_pairs: Array of premise and hypothesis input sentences.
        labels: Array of labels.
        batch_size: Integer batch size.
        shuffle: boolean, whether to shuffle the data.
        include_targets: boolean, whether to incude the labels.

    Returns:
        Tuples `([input_ids, attention_mask, `token_type_ids], labels)`
        (or just `[input_ids, attention_mask, `token_type_ids]`
         if `include_targets=False`)
    """

    def __init__(
        self,
        sentence_pairs,
        labels,
        batch_size=batch_size,
        shuffle=True,
        include_targets=True,
    ):
        self.sentence_pairs = sentence_pairs
        self.labels = labels
        self.shuffle = shuffle
        self.batch_size = batch_size
        self.include_targets = include_targets
        # Load our BERT Tokenizer to encode the text.
        # We will use base-base-uncased pretrained model.
        self.tokenizer = transformers.BertTokenizer.from_pretrained(
            "bert-base-uncased", do_lower_case=True
        )
        self.indexes = np.arange(len(self.sentence_pairs))
        self.on_epoch_end()

    def __len__(self):
        # Denotes the number of batches per epoch.
        return len(self.sentence_pairs) // self.batch_size

    def __getitem__(self, idx):
        # Retrieves the batch of index.
        indexes = self.indexes[idx * self.batch_size : (idx + 1) * self.batch_size]
        sentence_pairs = self.sentence_pairs[indexes]

        # With BERT tokenizer's batch_encode_plus batch of both the sentences are
        # encoded together and separated by [SEP] token.
        encoded = self.tokenizer.batch_encode_plus(
            sentence_pairs.tolist(),
            add_special_tokens=True,
            max_length=max_length,
            return_attention_mask=True,
            return_token_type_ids=True,
            pad_to_max_length=True,
            return_tensors="tf",
        )

        # Convert batch of encoded features to numpy array.
        input_ids = np.array(encoded["input_ids"], dtype="int32")
        attention_masks = np.array(encoded["attention_mask"], dtype="int32")
        token_type_ids = np.array(encoded["token_type_ids"], dtype="int32")

        # Set to true if data generator is used for training/validation.
        if self.include_targets:
            labels = np.array(self.labels[indexes], dtype="int32")
            return [input_ids, attention_masks, token_type_ids], labels
        else:
            return [input_ids, attention_masks, token_type_ids]

    def on_epoch_end(self):
        # Shuffle indexes after each epoch if shuffle is set to True.
        if self.shuffle:
            np.random.RandomState(42).shuffle(self.indexes)

def create_model():
    input_ids = tf.keras.layers.Input(
        shape=(max_length,), dtype=tf.int32, name="input_ids"
    )
    # Attention masks indicates to the model which tokens should be attended to.
    attention_masks = tf.keras.layers.Input(
        shape=(max_length,), dtype=tf.int32, name="attention_masks"
    )
    # Token type ids are binary masks identifying different sequences in the model.
    token_type_ids = tf.keras.layers.Input(
        shape=(max_length,), dtype=tf.int32, name="token_type_ids"
    )
    # Loading pretrained BERT model.
    bert_model = transformers.TFBertModel.from_pretrained("bert-base-uncased")
    # Freeze the BERT model to reuse the pretrained features without modifying them.
    bert_model.trainable = False

    sequence_output, pooled_output = bert_model(
        input_ids, attention_mask=attention_masks, token_type_ids=token_type_ids
    )
    # Add trainable layers on top of frozen layers to adapt the pretrained features on the new data.
    bi_lstm = tf.keras.layers.Bidirectional(
        tf.keras.layers.LSTM(64, return_sequences=True)
    )(sequence_output)
    # Applying hybrid pooling approach to bi_lstm sequence output.
    avg_pool = tf.keras.layers.GlobalAveragePooling1D()(bi_lstm)
    max_pool = tf.keras.layers.GlobalMaxPooling1D()(bi_lstm)
    concat = tf.keras.layers.concatenate([avg_pool, max_pool])
    dropout = tf.keras.layers.Dropout(0.3)(concat)
    output = tf.keras.layers.Dense(3, activation="softmax")(dropout)
    model = tf.keras.models.Model(
        inputs=[input_ids, attention_masks, token_type_ids], outputs=output
    )

    model.compile(
        optimizer=tf.keras.optimizers.Adam(),
        loss="categorical_crossentropy",
        metrics=["acc"],
    )
    return model


def check_similarity(sentence1, sentence2,model_question_answer):
    sentence_pairs = np.array([[str(sentence1), str(sentence2)]])
    test_data = BertSemanticDataGenerator(
        sentence_pairs, labels=None, batch_size=1, shuffle=False, include_targets=False,
    )

    proba = model_question_answer.predict(test_data)[0]
    idx = np.argmax(proba)
    proba = f"{proba[idx]: .2f}%"
    pred = labels[idx]
    return pred, proba

def question_answer(text,model_question_answer,fichier):
    translated = translate(text,src='fr', dest='en')
    liste_similarity=[]
    #print("translated",translated)
    for i in range(len(fichier)):
        if len(liste_similarity) <3:
            pred,proba = check_similarity(fichier.iloc[i][0],translated,model_question_answer)
            #print("pred :",pred,"proba",proba)
            if pred == "entailment":
                all_reponses = fichier.iloc[i][1]
                temp_liste = all_reponses.split("/")
                for temp in temp_liste:
                    liste_similarity.append(temp)
        else:
            return liste_similarity
    print(liste_similarity)
    return liste_similarity





def generate_sentences_french_gpt2(debut_phrase,num_return_sequences=1,length=20,temperature=1):
    response = french_generator(debut_phrase,num_return_sequences=1,max_length=length)
    liste = []
    for res in response:
        liste.append(res["generated_text"])
    return liste

def translate(texte,src="en",dest="fr"):
    translator = google_translator()  
    translate_text = translator.translate(texte,lang_tgt=dest)  
    return translate_text

def generate_sentences_english_gpt2(debut_phrase,english_generator,num_return_sequences,length,top_p):
    debut_phrase = translate(debut_phrase,'fr',dest = 'en')

    response_debut_phrase = english_generator(debut_phrase,num_return_sequences=num_return_sequences,max_length=length,top_p=top_p,top_k=30,do_sample=True)
    liste = []
    for res in response_debut_phrase:
        print("res",res)
        temp = translate(res["generated_text"],'en',dest = 'fr')
        temp = truncate(temp)
        liste.append(temp)
    return liste

def truncate(string):
    strin_clean = re.sub('\.(.*)', '.', string)
    strin_clean = re.sub('\?(.*)', '?', strin_clean)
    strin_clean = re.sub('\!(.*)', '!', strin_clean)
    return strin_clean


#----------Load model to finish sentences gpt-2 french fine tune----------
#french_generator = pipeline('text-generation',model='ml/modeles/gpt2-fine-tune', tokenizer='camembert-base')

#----------Load model to finish sentences gpt2-english not fine tune----------
def load_english_generator():
    english_generator = pipeline('text-generation', model='gpt2')
    return english_generator

#----------Load model for questions answer----------
def load_bert_model():
    model = create_model()
    path = "ml/modeles/bert-question-reponses/weights"
    model.load_weights(path)
    return model

def load_file():
    fichier = pd.read_csv("ml/dataset_questions_reponses.txt", sep=";")
    return fichier