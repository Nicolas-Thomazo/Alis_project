Flask api
#########

Les modèles de machine lmearning utilisés par l'application sont sur un serveur flask qui utilise python.

Ci-dessous, les différents ports de l'api, avec le format json à leur envoyer.

Générer des fin de phrases à l'aide de gpt-2 anglais (traduit en français ensuite)::

   json =   {
              "id" : 1,
              "debut_phrase" : "j'aime aller"
            }
   "generate_sentences_english_gpt2"

Générer des fin de phrases à l'aide de gpt-2 fine tune en français::
   
   json =   {
              "id" : 1,
              "debut_phrase" : "j'aime aller"
            }
   "generate_sentences_french_gpt2"

Insérer une phrase dans une base de données. Pour cette partie, il faut rentrer dans le fichier config.json (dans backend/config.json), et changer le nom de la base de données, user, password etc pour pouvoir mettre la base de données souhaité::

   json =   {
              "interlocuteur" : "Jean",
              "phrase" : "Tu as fait quoi aujourd'hui",
              "reponse" : "Je suis allé me balader"
            }
   "insert_to_dabatase"

Répondre automatiquement à des questions::

   json =   {
              "id" : 1,
              "phrase" : "Est-ce que tu as faim ?",
            }
   "questions_reponses"
