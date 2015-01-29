Int�grer Navegar � une solution WPF

Bonjour, bienvenue dans ce tutoriel vous permettant d'int�grer Navegar dans une solution WPF. Nous allons voir l'installation du package, l'initialisation et la navigation vers un premier ViewModel
I. Cr�ation d'une solution Windows 8.1 et installation de Navegar

Common�ons par cr�er une solution de type WPF
Pour cela, il vous suffit de faire

Fichier ? Nouveau ? Projet

ou cliquez ici : Cr�er une nouvelle solution

Une fois votre solution cr��e, nous allons proc�der � l'installation de Navegar, pour cela vous pouvez utiliser le gestionnaire de paquet de NuGet en mode graphique ou bien en ligne de commande
install-package Navegar Copier

Ceci installera �galement MvvmLightLibs

Vous devez �galement installer MvvmLight complet pour le bon fonctionnement de l'application, Navegar ne l'installe pas car il est possible d'utiliser Navegar dans un projet biblioth�que qui n'a pas besoin de tout MvvmLight
II. Initialisation de la navigation

La navigation en WPF fonctionne sur le principe d'une page principale qui va accueillir des UserControl qui seront vos diff�rentes pages. Navegar ne g�re pas de navigation multi-fen�tres.

Pour que cela fonctionne il faut modifier le fichier ViewModelLocator de MvvmLight pour enregistrer la classe de navigation dans l'IOC de MvvmLight et enregistrer l'instance de la fen�tre principale (h�te)

Modifiez le constructeur de la classe ViewModelLocator et ajoutez :
//1. Enregistrer la classe de navigation dans l'IOC
SimpleIoc.Default.Register<INavigation, Navigation>();

//2. G�n�rer le viewmodel principal, le type du viewmodel
//peut �tre n'importe lequel
//Cette g�n�ration va permettre de cr�er au sein de la
//navigation, une instance unique pour le viewmodel principal,
//qui sera utilis�e par la classe de navigation
SimpleIoc.Default.GetInstance<INavigation>()
.GenerateMainViewModelInstance<MainViewModel>(); Copier

Supprimez la ligne :
SimpleIoc.Default.Register<MainViewModel>();

Afin d'affecter un DataContext � la page principale vous devez enregistrer une propri�t� dans la classe ViewModelLocator. Ajoutez le code dans la classe (en remplacement de la propri�t� d�ja d�finie par MvvmLight) :
public YourFirstViewModel Main
{
 //3. Retrouve le viewmodel principal
 get
 {
  return SimpleIoc.Default.GetInstance<INavigation>().GetMainViewModelInstance<MainViewModel>();
 }
} Copier

Normalement MvvmLight a d�ja affect� � la page principale la propri�t�, si ce n'est pas le cas affectez cette propri�t� comme DataContext de la page
<Window.DataContext>
 <Binding Path="Main" Source="{StaticResource Locator}"/>
</Window.DataContext> Copier

La navigation est maintenant initialis�e.
III. Pr�paration de la page Main et du MainViewModel pour g�rer la navigation en Usercontrol

Ajouter un USerControl et un ViewModel associ�, nommez les FirstView et FirstViewModel

Afin d'avoir une navigation par UserControl vous devez modifier votre Main.xaml et votre MainViewModel.cs en utilisant le code suivant.

MainViewModel.cs
public class MainViewModel : ViewModelBase
{
private readonly DispatcherTimer _timerLoadAccueil;

/// <summary>
/// Permet de contr�ler la propri�t� CurrentView
/// </summary>
private ViewModelBase _currentView;

/// <summary>
/// L'attribut CurrentViewNavigation permet de d�finir automatiquement, quelle propri�t� du viewmodel
/// devra �tre utilis� pour charger le viewmodel vers lequel la navigation va s'effectuer
/// </summary>
[CurrentViewNavigation]
public ViewModelBase CurrentView
{
get { return _currentView; }
set
{
_currentView = value;
RaisePropertyChanged("CurrentView");
}
}

public MainViewModel()
{ //Permet d'�viter l'appel � la navigation pendant le chargement du viewmodel principal. Sans ceci le chargement du MainViewModel
//ne se passe pas correctement puisque le chargement du viewmodel n'est pas possible
_timerLoadAccueil = new DispatcherTimer
{
Interval = new TimeSpan(0, 0, 0, 0, 500)
};
_timerLoadAccueil.Tick += LoadAccueil;
_timerLoadAccueil.Start();
}

/// <summary>
/// Navigation vers le premier viewmodel
/// </summary>
///
///
private void LoadAccueil(object sender, EventArgs e)
{
_timerLoadAccueil.Stop();
SimpleIoc.Default.GetInstance<INavigation>().NavigateTo<FirstViewModel>();
}
}
Copier

Main.xaml
<Grid> <ContentControl Content="{Binding CurrentView}" Focusable="False" Grid.Row="1"/> </Grid> Copier

La navigation utilisera la propri�t� CurrentView afin d'int�grer la page associ�e au ViewModel vers lequel la navigation sera dirig�. Un ContentControl doit donc �tre plac� dans la page afin d'afficher le UserControl.

 

Pour associer le ViewModel d'un UserControl au UserControl vous devez ajouter un DataTemplate, ainsi lorsque Navegar fera l'instanciation du ViewModel le UserControl lui sera associ�.
<DataTemplate DataType="{x:Type ViewModel:YourFirstViewModel}">
 <View:YourFirstView />
</DataTemplate> Copier
IV. Premi�re navigation vers une seconde page

Ajoutez un second UserControl avec son ViewModel associ�, nommez les SecondViewModelet SecondView. Ajoutez un DataTemplate pour associer les deux

Ajoutez un bouton � votre premier ViewModel afin de lancer la navigation vers le second. Pour cela cr�ez une ICommand et affectez le code suivant � votre ICommand :
SimpleIoc.Default.GetInstance<INavigation>().NavigateTo<SecondViewModel>(this, new object[] {}, true); Copier

Afin de revenir vers le premier ViewModel il vous faut ajouter un bouton qui vous permettra d'affecter une ICommand. Dans cette ICommand affectez le code suivant
if (SimpleIoc.Default.GetInstance<INavigation>().CanGoBack())
{
 SimpleIoc.Default.GetInstance<INavigation>().GoBack();
} Copier

Voila votre navigation est maintenant en place de fa�on simple. Pour plus de d�tails sur les fonctionnalit�s de la fonction NavigateTo et GoBack vous pouvez consulter la documentation de code
