Modèles développés
##################

Il existe 3 modèles différents que nous avons utilisés.
Dans le dossier entrainements_modeles, vous pourrez trouver des notebook jupyter qui permettent de ré-entrainer ces modèles pour qu'ils soient plus performants.


Génération de texte
**********************
Nous nous sommes donc concentrés sur GPT-2. Il a cependant été uniquement entraîné en anglais. Nous avions donc 2 possibilités.

- Solution 1
   La première possibilité est de traduire le début de la phrase écrite par la personne LIS, puis de la donner au modèle afin qu’il génère une fin de phrase logique. Il faut ensuite re-traduire la fin de phrase et l’afficher sur l’interface. Cette solution à l'avantage d’utiliser le modèle GPT-2 tel quel, qui est l’un des meilleurs pour la prédiction. En revanche, il introduit des biais à cause de la double traduction anglais/français. 
   Nous avons utilisé pour la traduction une API gratuite, il y a cependant la possibilité d’utiliser l’API payante de Deepl qui est très utilisée et fait de très bonnes traductions.


- Solution 2
   La seconde possibilité est de “fine-tune” le modèle anglais pour qu’il puisse prédire en français. Il faut donc lui “apprendre” le français en lui donnant de nombreux textes écrits en français. L’avantage de cette solution est que le modèle créé ne possède plus le biais dû à la traduction. 	C’est en revanche une tâche fastidieuse qui nécessite de nombreux textes en français ( plusieurs giga de données) et une puissance informatique conséquente. Nous avons utilisé les sous-titres de films car ils sont censés être représentatifs de la réalité. Nous avons pris les 10 saisons de la série « Friends », mais ces données ne représentaient que 5 Mo de discussion.

Réponses aux questions
**********************

La phrase est prise en compte dans le JSON envoyé par l’application. Elle est ensuite traduite en anglais. Puis, le calcul de similarité est effectué pour chaque phrase qui compose le dataset avec la phrase issue de l’application. Le programme choisit les phrases qui ont le plus de similarité avec celles posées et les envoie par la suite pour répondre à l’appel de l’application.

**Points importants**

Le modèle de réponse aux questions a été entrainé avec Bert. C'est un modèle qui est lourd et qui prends beaucoup de temps à tourner. 
2 axes d'améliorations.

- Entrainer avec un modèle plus léger comme *distilBert*
- Changer la taille des tokens en entrée

Concernant gpt-2, la meilleur solution serait de continuer à fine-tune le modèle français. Pour cela il faut 2 choses

- Utiliser une machine puissante (sur azure par exemple) qui utilise un GPU. Colab arrête la session au bout d'un certains temps ce n'est donc pas le meilleur environment. 
- Récuperer d'auter sous-titres de films pour entrainer le modèle sur plus de données. L'objectif sera d'entrainer le modèle sur les phrases dites par la personne LIS.

