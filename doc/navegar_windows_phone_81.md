
Principe de fonctionnement sur Windows Phone 8.1 (XAML)

Bonjour, bienvenue dans ce tutoriel vous permettant d'int�grer Navegar dans une solution Windows Phone 8.1. Nous allons voir l'installation du package, l'initialisation et la navigation vers un premier ViewModel
I. Cr�ation d'une solution Windows Phone 8.1 et installation de Navegar

Common�ons par cr�er une solution de type Windows Phone 8.1
Pour cela, il vous suffit de faire

Fichier ? Nouveau ? Projet

Une fois votre solution cr��e, nous allons proc�der � l'installation de Navegar, pour cela vous pouvez utiliser le gestionnaire de paquet de NuGet en mode graphique ou bien en ligne de commande
install-package Navegar

Ceci installera �galement MvvmLightLibs

Vous devez �galement installer MvvmLight complet pour le bon fonctionnement de l'application, Navegar ne l'installe pas car il est possible d'utiliser Navegar dans un projet biblioth�que qui n'a pas besoin de tout MvvmLight
II. Initialisation de la navigation

Afin d'initialiser la navigation au sein de l'application vous devez modifier le fichier App.xaml.cs pour naviguer sur votre premier ViewModel (premi�re page)

Modifiez la fonction OnLaunched

En remplacement des lignes
if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
{
 throw new Exception("Failed to create initial page");
}

Ajoutez :
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
 ...

 if (rootFrame.Content == null)
 {
  ...

  //Initialisation de la navigation et navigation vers le premier ViewModel
  SimpleIoc.Default.GetInstance<INavigation>().InitializeRootFrame(rootFrame);

  //Surcharge �ventuelle de la prise en compte du bouton physique ou virtuel de retour
  //Par d�faut Navegar par en charge ce bouton
  //SimpleIoc.Default.GetInstance<INavigation>().BackButtonPressed += VotreFonctionDeRetour;

  if (string.IsNullOrEmpty(SimpleIoc.Default.GetInstance<INavigation>().NavigateTo<YourFirstViewModel>(true)))
  {
   throw new Exception("Impossible de cr�er la page d'accueil");
  }
 }
}

Pour que cela fonctionne il faut modifier le fichier ViewModelLocator de MvvmLight pour enregistrer la classe de navigation ainsi que vos ViewModels et leur page associ�e dans l'IOC de MvvmLight.
L'enregistrement des ViewModel et des pages devra �tre fait � chaque cr�ation d'une page.

Modifiez le constructeur de la classe ViewModelLocator et ajoutez :
//Enregistrer la classe de navigation dans l'IOC et les ViewModels
if (!SimpleIoc.Default.IsRegistered<INavigation>())
{
 SimpleIoc.Default.Register<INavigation, Navigation>();
}

//Enregistrer le ViewModel avec sa page associ�e
SimpleIoc.Default.GetInstance<INavigation>().RegisterView<YourFirstViewModel, YourFirstPage>();

Supprimez la ligne :
SimpleIoc.Default.Register<YourFirstViewModel>();

Pas d'inqui�tude le viewmodel YourFirstViewModel sera enregistr� au premier appel vers celui-�i dans la classe de navigation

Afin d'affecter un DataContext � la page associ�e vous devez enregistrer une propri�t� dans la classe ViewModelLocator. Ajoutez le code alors le code suivant dans la classe (en remplacement de la propri�t� d�ja d�finie par MvvmLight) :
public YourFirstViewModel YourFirstViewModelInstance
{
 get { return SimpleIoc.Default.GetInstance<INavigation>().GetViewModelInstance<YourFirstViewModel>(); }
}

Il faut maintenant affecter cette propri�t� comme DataContext de la page
<Page.DataContext>
 <Binding Path="YourFirstViewModelInstance" Source="{StaticResource Locator}"/>
</Page.DataContext>

La navigation est maintenant initialis�e et votre premier ViewModel et premi�re Page sont pr�ts � �tre utilis�s
III. Premi�re navigation vers une seconde page

Ajoutez une seconde page avec son ViewModel associ�e, nommez les SecondViewModelPage et SecondPage

Cr�er une propri�t� d'instance comme pour le premier ViewModel et affectez cette propri�t� en tant que DataContext de la page

Ajoutez un bouton � votre premier ViewModel afin de lancer la navigation vers la seconde page. Pour cela cr�ez une ICommand et affectez le code suivant � votre ICommand :
SimpleIoc.Default.GetInstance<INavigation>().NavigateTo<SecondViewModelPage>(this, new object[] {}, true);

Voila votre navigation est maintenant en place de fa�on simple. Pour plus de d�tails sur les fonctionnalit�s de la fonction NavigateTo vous pouvez consulter la documentation de code
