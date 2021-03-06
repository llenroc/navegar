// <copyright file="Navigation.cs" company="Kopigi">
// Copyright � Kopigi 2013
// </copyright>
//  ****************************************************************************
// <author>Marc PLESSIS</author>
// <date>26/10/2013</date>
// <project>Navegar.Win8</project>
// <web>http://www.kopigi.fr</web>
// <license>
// The MIT License (MIT)
// 
// Copyright (c) 2013 Marc Plessis
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
//  </license>

#region using

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
#if WINDOWS_PHONE
using Microsoft.Phone.Controls;
using System.Windows.Controls;
#else
using System;

#endif

#endregion

namespace Navegar.UAP.Win81
{
    /// <summary>
    /// Permet d'ex�cuter une action avant la navigation
    /// </summary>
    public delegate bool PreNavigateDelegate<T>(T currentViewModelInstance, Type currentViewModel, Type viewModelToNavigate) where T : ViewModelBase;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void EventHandler(object sender, EventArgs args);

    /// <summary>
    /// Impl�mentation de la classe de navigation
    /// </summary>
    /// <example>
    ///   On r�alise la navigation suivante :
    /// 
    ///   MainViewModel -&gt; FirstViewModel &lt;-&gt; SecondViewModel
    ///  
    ///   <code>
    /// Dans le ViewModelLocator.cs :
    /// 
    /// public ViewModelLocator()
    /// {
    ///    ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
    ///
    ///    //Enregistrer la classe de navigation dans l'IOC et les ViewModels
    ///    SimpleIoc.Default.Register&lt;INavigation, Navigation&gt;();
    ///    SimpleIoc.Default.Register&lt;MainViewModel&gt;();
    ///
    ///    //Association des vues avec leur mod�le de vue
    ///    SimpleIoc.Default.GetInstance&lt;INavigation&gt;().RegisterView&lt;BlankPage1ViewModel, BlankPage1&gt;();
    ///    SimpleIoc.Default.GetInstance&lt;INavigation&gt;().RegisterView&lt;BlankPage2ViewModel, BlankPage2&gt;();
    ///  }
    ///
    ///  public MainViewModel Main
    ///  {
    ///     get { return SimpleIoc.Default.GetInstance&lt;MainViewModel&gt;(); }
    ///  }
    ///
    ///  public BlankPage1ViewModel BlankPage1ViewModel
    ///  {
    ///     get { return SimpleIoc.Default.GetInstance&lt;INavigation&gt;().GetViewModelInstance&lt;BlankPage1ViewModel&gt;(); }
    ///  }
    ///
    ///  public BlankPage2ViewModel BlankPage2ViewModel
    ///  {
    ///     get { return SimpleIoc.Default.GetInstance&lt;INavigation&gt;().GetViewModelInstance&lt;BlankPage2ViewModel&gt;(); }
    ///  }
    /// 
    /// 
    /// Dans MainViewModel.cs :
    /// 
    ///    //Pour aller vers un autre ViewModel
    ///    SimpleIoc.Default.GetInstance&lt;INavigation&gt;().NavigateTo&lt;FirstViewModel&gt;();
    /// 
    /// 
    /// 
    /// Dans BlankPage1ViewModel.cs :
    /// 
    ///    //Pour aller vers SecondViewModel.cs, en supposant que le constructeur prenne un argument et que l'on veuille revenir vers FirstViewModel
    ///    SimpleIoc.Default.GetInstance&lt;INavigation&gt;().NavigateTo&lt;BlankPage2ViewModel&gt;(this, new object[] { Data }, true);
    /// 
    /// 
    /// 
    /// Dans BlankPage2ViewModel.cs :
    /// 
    ///    //Pour revenir vers BlankPage1ViewModel :
    ///    if(SimpleIoc.Default.GetInstance&lt;INavigation&gt;().CanGoBack())
    ///    {
    ///       SimpleIoc.Default.GetInstance&lt;INavigation&gt;().GoBack();
    ///    }
    ///   </code>
    /// </example>
    public class Navigation : INavigation
    {
        #region fields

        private readonly Dictionary<Type, string> _factoriesInstances = new Dictionary<Type, string>();
        private readonly Dictionary<Type, Type> _historyInstances = new Dictionary<Type, Type>();
        private Type _currentViewModel;
#if WINDOWS_PHONE
        private PhoneApplicationFrame _rootFrame;
        private readonly Dictionary<Type, System.Uri> _historyNavigation = new Dictionary<Type, System.Uri>(); 
        private readonly Dictionary<Type, System.Uri> _viewsRegister = new Dictionary<Type, System.Uri>();
#else
        private Frame _rootFrame;
        private string _navigationStateInitial;
        private readonly Dictionary<Type, string> _historyNavigation = new Dictionary<Type, string>();
        private readonly Dictionary<Type, Type> _viewsRegister = new Dictionary<Type, Type>();
#endif

        #endregion

        /// <summary>
        /// Permet d'indiquer que la navigation est annul�e
        /// </summary>
        public event EventHandler NavigationCanceledOnPreviewNavigate;

        /// <summary>
        /// Permet d'ex�cuter une action avant la navigation
        /// </summary>
        public PreNavigateDelegate<ViewModelBase> PreviewNavigate { get; set; }

#if WINDOWS_PHONE_APP
        private System.EventHandler<Windows.Phone.UI.Input.BackPressedEventArgs> _backButtonPressed;
        /// <summary>
        /// Evenement de navigation arri�re avec le bouton physique ou virtuel
        /// Permet de d�finir soit m�me une fonction g�rant ce retour sans utiliser celui par d�faut de Navegar
        /// </summary>
        public System.EventHandler<Windows.Phone.UI.Input.BackPressedEventArgs> BackButtonPressed
        {
            get { return _backButtonPressed; }
            set
            {
                _backButtonPressed = value;
                if (value != null)
                {
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtonsBackPressed;
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed += _backButtonPressed;    
                }
                else
                {
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed -= _backButtonPressed;
                    Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtonsBackPressed; 
                }
            }
        }
#endif

#if WINDOWS_PHONE
        /// <summary>
        /// Permet de r�f�rencer la Frame Principale g�n�r� au lancement de l'application, pour la suite de la navigation
        /// </summary>
        /// <param name="rootFrame">Frame de navigation principale</param>
        public void InitializeRootFrame(PhoneApplicationFrame rootFrame)
        {
            _rootFrame = rootFrame;
        }

#endif

#if WINDOWS_APP || NETFX_CORE

        /// <summary>
        /// Permet de r�f�rencer la Frame Principale g�n�r� au lancement de l'application, pour la suite de la navigation
        /// </summary>
        /// <param name="rootFrame">Frame de navigation principale</param>
        public void InitializeRootFrame(Frame rootFrame)
        {
            _navigationStateInitial = rootFrame.GetNavigationState();
            _rootFrame = rootFrame;
#if WINDOWS_PHONE_APP
            //Gestion du bouton de retour physique ou virtuel du device
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtonsBackPressed;
#endif
        }
#endif
        /// <summary>
        /// Naviguer vers un ViewModel 
        /// </summary>
        /// <typeparam name="TTo">
        /// Type du Viewmodel vers lequel la navigation est effectu�e
        /// </typeparam>
        /// <param name="parametersViewModel">
        /// Tableau des param�tres �ventuels � transmettre au constructeur du ViewModel
        /// </param>
        /// <returns>
        /// Retourne la cl� unique pour SimpleIoc, de l'instance du viewmodel vers lequel la navigation a eu lieu
        /// </returns>
        public string NavigateTo<TTo>(params object[] parametersViewModel) where TTo : class
        {
            return Navigate<TTo>(typeof(TTo), null, parametersViewModel, null, null);
        }

        /// <summary>
        /// Naviguer vers un ViewModel 
        /// </summary>
        /// <typeparam name="TTo">
        /// Type du Viewmodel vers lequel la navigation est effectu�e
        /// </typeparam>
        /// <param name="newInstance">
        /// Indique si l'on g�n�re une nouvelle instance obligatoirement
        /// </param>
        /// <param name="parametersViewModel">
        /// Tableau des param�tres �ventuels � transmettre au constructeur du ViewModel
        /// </param>
        /// <returns>
        /// Retourne la cl� unique pour SimpleIoc, de l'instance du viewmodel vers lequel la navigation a eu lieu
        /// </returns>
        public string NavigateTo<TTo>(bool newInstance, params object[] parametersViewModel) where TTo : class
        {
            return Navigate<TTo>(typeof(TTo), null, parametersViewModel, null, null, newInstance);
        }

        /// <summary>
        /// Naviguer vers un ViewModel
        /// Le param�tre <param name="functionToLoad"></param> permet de sp�cifier un nom de fonction � appeler apr�s le chargement du viewModel cibl�
        /// </summary>
        /// <typeparam name="TTo">
        /// Type du Viewmodel vers lequel la navigation est effectu�e
        /// </typeparam>
        /// <param name="parametersViewModel">
        /// Tableau des param�tres �ventuels � transmettre au constructeur du ViewModel
        /// </param>
        /// <param name="functionToLoad">
        /// Permet de sp�cifier un nom de fonction � appeler apr�s le chargement du viewModel cibl�
        /// </param>
        /// <param name="parametersFunction">
        /// Param�tres pour la fonction appel�e
        /// </param>
        /// <param name="newInstance">
        /// Indique si l'on g�n�re une nouvelle instance obligatoirement
        /// </param>     
        /// <returns>
        /// Retourne la cl� unique pour SimpleIoc, de l'instance du viewmodel vers lequel la navigation a eu lieu
        /// </returns>  
        public string NavigateTo<TTo>(object[] parametersViewModel, string functionToLoad, object[] parametersFunction, bool newInstance = false) where TTo : class
        {
            return Navigate<TTo>(typeof(TTo), null, parametersViewModel, functionToLoad, parametersFunction, newInstance);
        }

        /// <summary>
        /// Naviguer vers un ViewModel, avec un ViewModel en historique pr�c�dent
        /// </summary>
        /// <typeparam name="TTo">
        /// Type du Viewmodel vers lequel la navigation est effectu�e
        /// </typeparam>
        /// <param name="currentInstance">
        /// Viewmodel depuis lequel la navigation est effectu�e
        /// </param>
        /// <param name="parametersViewModel">
        /// Tableau des param�tres �ventuels � transmettre au constructeur du ViewModel
        /// </param>
        /// <param name="newInstance">
        /// Indique si l'on g�n�re une nouvelle instance obligatoirement
        /// </param>
        /// <returns>
        /// Retourne la cl� unique pour SimpleIoc, de l'instance du viewmodel vers lequel la navigation a eu lieu
        /// </returns>
        public string NavigateTo<TTo>(ViewModelBase currentInstance, object[] parametersViewModel, bool newInstance = false) where TTo : class
        {
            return Navigate<TTo>(typeof(TTo), currentInstance.GetType(), parametersViewModel, null, null, newInstance);
        }

        /// <summary>
        /// Navigeur vers un ViewModel, avec un ViewModel en historique pr�c�dent. 
        /// Le param�tre <param name="functionToLoad"></param> permet de sp�cifier un nom de fonction � appeler apr�s le chargement du viewModel cibl�
        /// </summary>
        /// <typeparam name="TTo">
        /// Type du Viewmodel vers lequel la navigation est effectu�e
        /// </typeparam>
        /// <param name="currentInstance">
        /// Viewmodel depuis lequel la navigation est effectu�e
        /// </param>
        /// <param name="parametersViewModel">
        /// Tableau des param�tres �ventuels � transmettre au constructeur du ViewModel
        /// </param>
        /// <param name="functionToLoad">
        /// Permet de sp�cifier un nom de fonction � appeler apr�s le chargement du viewModel cibl�
        /// </param>
        /// <param name="parametersFunction">
        /// Param�tres pour la fonction appel�e
        /// </param>
        /// <param name="newInstance">
        /// Indique si l'on g�n�re une nouvelle instance obligatoirement
        /// </param>
        /// <returns>
        /// Retourne la cl� unique pour SimpleIoc, de l'instance du viewmodel vers lequel la navigation a eu lieu
        /// </returns>
        public string NavigateTo<TTo>(ViewModelBase currentInstance, object[] parametersViewModel, string functionToLoad, object[] parametersFunction, bool newInstance = false) where TTo : class
        {
            return Navigate<TTo>(typeof(TTo), currentInstance.GetType(), parametersViewModel, functionToLoad, parametersFunction, newInstance);
        }

        /// <summary>
        /// D�terminer si un historique est possible depuis le ViewModel en cours
        /// </summary>
        /// <returns>
        /// <c>true</c> si la navigation est possible, sinon <c>false</c>
        /// </returns>
        public bool CanGoBack()
        {
            return _currentViewModel != null && _historyInstances.ContainsKey(_currentViewModel) && CanGoBackFrame();
        }

        /// <summary>
        /// Naviguer vers l'historique (ViewModel pr�c�dent) depuis le ViewModel en cours, si une navigation arri�re est possible
        /// </summary>
        public void GoBack ()
        {
            if (CanGoBack()) 
            {
                if(_historyInstances.ContainsKey(_currentViewModel) && CanGoBackFrame())
                {
                    Type viewModelFrom;
                    if(_historyInstances.TryGetValue(_currentViewModel, out viewModelFrom))
                    {
                        Navigate(viewModelFrom, null, null);
                    }   
                }
            }
        }

        /// <summary>
        /// Naviguer vers l'historique (ViewModel pr�c�dent) depuis le ViewModel en cours, si une navigation arri�re est possible
        /// </summary>
        /// /// <param name="functionToLoad">
        /// Permet de sp�cifier un nom de fonction � appeler apr�s le chargement du viewModel cibl�
        /// </param>
        /// <param name="parametersFunction">
        /// Param�tres pour la fonction appel�e
        /// </param>
        public void GoBack(string functionToLoad, params object[] parametersFunction)
        {
            if (CanGoBack())
            {
                if (_historyInstances.ContainsKey(_currentViewModel) && CanGoBackFrame())
                {
                    Type viewModelFrom;
                    if (_historyInstances.TryGetValue(_currentViewModel, out viewModelFrom))
                    {
                        Navigate(viewModelFrom, functionToLoad, parametersFunction);    
                    }
                }
            }
        }

        /// <summary>
        /// Permet de connaitre le type du ViewModel au niveau n-1 de l'historique de navigation
        /// </summary>
        /// <returns>Type du ViewModel</returns>
        public Type GetTypeViewModelToBack()
        {
            if (CanGoBack())
            {
                if (_historyInstances.ContainsKey(_currentViewModel) && CanGoBackFrame())
                {
                    Type viewModelFrom;
                    if (_historyInstances.TryGetValue(_currentViewModel, out viewModelFrom))
                    {
                        return viewModelFrom;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// R�cup�re l'instance du ViewModel
        /// </summary>
        /// <typeparam name="T">
        /// Type du ViewModel
        /// </typeparam>
        /// <returns>
        /// Instance du ViewModel
        /// </returns>
        public T GetViewModelInstance<T>() where T : ViewModelBase
        {
            if (_factoriesInstances.ContainsKey(typeof(T)))
            {
                string key;
                var result = _factoriesInstances.TryGetValue(typeof(T), out key);
                if (result)
                {
                    return SimpleIoc.Default.GetInstance<T>(key);
                }
            }
            return null;
        }

#if WINDOWS_PHONE

        /// <summary>
        /// Permet d'associer un type pour la vue � un type pour le mod�le de vue
        /// </summary>
        public void RegisterView<TViewModel>(System.Uri uri)
            where TViewModel : ViewModelBase
        {
            
            if (!_viewsRegister.ContainsKey(typeof(TViewModel)))
            {
                _viewsRegister.Add(typeof(TViewModel), uri);
            }
        }
#else
        /// <summary>
        /// Permet d'associer un type pour la vue � un type pour le mod�le de vue
        /// </summary>
        public void RegisterView<TViewModel, TView>()
            where TViewModel : ViewModelBase
            where TView : Page
        {
            if (!_viewsRegister.ContainsKey(typeof(TViewModel)))
            {
                _viewsRegister.Add(typeof(TViewModel), typeof(TView));
            }
        }

        /// <summary>
        /// Permet d'associer un type pour la vue � un type pour le mod�le de vue
        /// </summary>
        [Obsolete("Use RegisterView<TViewModel, TView>()", false)]
        public void RegisterView(Type viewModel, Type view)
        {
            if (!_viewsRegister.ContainsKey(viewModel))
            {
                _viewsRegister.Add(viewModel, view);
            }
        }
#endif

        /// <summary>
        /// Permet de retrouver l'instance du ViewModel courant
        /// </summary>
        /// <returns>ViewModel courant</returns>
        public ViewModelBase GetViewModelCurrent()
        {
            if (_factoriesInstances.ContainsKey(_currentViewModel))
            {
                string key;
                if (_factoriesInstances.TryGetValue(_currentViewModel, out key))
                {
                    return (ViewModelBase)SimpleIoc.Default.GetInstance(_currentViewModel, key);
                }
            }
            return null;
        }

        /// <summary>
        /// D�clenche l'�v�nement d'annulation de navigation
        /// </summary>
        public void CancelNavigation()
        {
            if (NavigationCanceledOnPreviewNavigate != null)
            {
                NavigationCanceledOnPreviewNavigate(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Permet de vider l'historique de navigation
        /// </summary>
        public void Clear()
        {
            ClearNavigation();
        }

        public void Dispose()
        {
            ClearNavigation();
            GC.Collect();
        }

        #region private

        /// <summary>
        /// Naviguer vers l'historique (ViewModel pr�c�dent) depuis le ViewModel en cours
        /// </summary>
        /// <param name="viewModelToName">
        /// Type du Viewmodel vers lequel la navigation est effectu�e
        /// </param>
        /// <param name="functionToLoad">
        /// Permet de sp�cifier un nom de fonction � appeler apr�s le chargement du viewModel cibl�
        /// </param>
        /// <param name="parametersFunction">
        /// Param�tres pour la fonction appel�e
        /// </param>
        private void Navigate(Type viewModelToName, string functionToLoad, object[] parametersFunction)
        {
            if (viewModelToName != null)
            {
                string key = String.Empty;

                if (_factoriesInstances.ContainsKey(viewModelToName))
                {
                    _factoriesInstances.TryGetValue(viewModelToName, out key);
                }

                if (!String.IsNullOrEmpty(key))
                {
                    var instance = (ViewModelBase)SimpleIoc.Default.GetInstance(viewModelToName, key);
                    if (instance != null)
                    {
                        var result = SetGoBack(viewModelToName);
                        if (result)
                        {
                            _currentViewModel = viewModelToName;

                            //Gestion d'une fonction � appeler suite � la g�n�ration de l'instance
                            //La recherche de la m�thode doit se faire sur le type de l'instance
                            if (!string.IsNullOrEmpty(functionToLoad))
                            {
                                var method = instance.GetType().GetMethod(functionToLoad, parametersFunction);
                                if (method != null)
                                {
                                    method.Invoke(instance, parametersFunction);    
                                }
                                else
                                {
                                    var countParameters = parametersFunction != null
                                                  ? ((IEnumerable<object>)parametersFunction).Count()
                                                  : 0;
                                    throw new FunctionToLoadNavigationException(string.Format("{0} with {1} parameter(s)", functionToLoad, countParameters), instance.GetType().Name);
                                }
                            }
                        }
                        else
                        {
                            throw new HistoryNavigationException();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Naviguer vers un ViewModel 
        /// </summary>
        /// <typeparam name="TTo">
        /// Type du Viewmodel vers lequel la navigation est effectu�e
        /// </typeparam>
        /// <param name="viewModelToName">
        /// Type du Viewmodel vers lequel la navigation est effectu�e
        /// </param>
        /// <param name="viewModelFromName">
        /// Type du Viewmodel depuis lequel la navigation est effectu�e
        /// </param>
        /// <param name="parametersViewModel">
        /// Tableau des param�tres �ventuels � transmettre au constructeur du ViewModel
        /// </param>
        /// <param name="functionToLoad">
        /// Permet de sp�cifier un nom de fonction � appeler apr�s le chargement du viewModel cibl�
        /// </param>
        /// <param name="parametersFunction">
        /// Param�tres pour la fonction appel�e
        /// </param>
        /// <param name="newInstance">
        /// Indique si l'on g�n�re une nouvelle instance obligatoirement
        /// </param>
        /// <returns>
        /// Retourne la cl� unique pour SimpleIoc, de l'instance du viewmodel vers lequel la navigation a eu lieu
        /// </returns>
        private string Navigate<TTo>(Type viewModelToName, Type viewModelFromName, object[] parametersViewModel, string functionToLoad, object[] parametersFunction, bool newInstance = false) where TTo : class
        {
            try
            {
                //Pr�-navigation
                if (PreviewNavigate != null)
                {
                    ViewModelBase currentInstance = null;
                    if (viewModelFromName != null)
                    {
                        if (_factoriesInstances.ContainsKey(viewModelToName))
                        {
                            string keyInstance;
                            if (_factoriesInstances.TryGetValue(viewModelToName, out keyInstance))
                            {
                                currentInstance = (ViewModelBase)SimpleIoc.Default.GetInstance(viewModelToName, keyInstance);
                            }
                        }
                    }

                    if (!PreviewNavigate(currentInstance, viewModelFromName, viewModelToName))
                    {
                        if (NavigationCanceledOnPreviewNavigate != null)
                        {
                            NavigationCanceledOnPreviewNavigate(this, EventArgs.Empty);
                        }
                        return string.Empty;
                    }
                }

                //V�rification du type de ViewModel demand� pour l'historique
                if (!viewModelToName.GetTypeInfo().IsSubclassOf(typeof(ViewModelBase)))
                {
                    throw new ViewModelHistoryTypeException(viewModelToName.ToString());
                }

                //Gestion de l'historique
                if (viewModelFromName != null)
                {
                    if (_historyInstances.ContainsKey(viewModelToName))
                    {
                        _historyInstances[viewModelToName] = viewModelFromName;
                    }
                    else
                    {
                        _historyInstances.Add(viewModelToName, viewModelFromName);
                    }

                    //Gestion de l'historique de navigation
                    SetNavigationHistory(viewModelFromName);
                }

                //G�n�ration d'une instance du viewmodel
                string key;
                if (_factoriesInstances.ContainsKey(viewModelToName) && !newInstance)
                {
                    _factoriesInstances.TryGetValue(viewModelToName, out key);
                }
                else
                {
                    if(_factoriesInstances.ContainsKey(viewModelToName))
                    {
                        //Suppression de l'instance du viewModel dans le cache de SimpleIOC
                        _factoriesInstances.TryGetValue(viewModelToName, out key);

                        if (key != null)
                        {
                            _factoriesInstances.Remove(viewModelToName);
                            SimpleIoc.Default.Unregister<TTo>(key);    
                        }
                    }

                    var instanceNew = Activator.CreateInstance(viewModelToName, parametersViewModel);
                    key = Guid.NewGuid().ToString();
                    SimpleIoc.Default.Register<TTo>(() => (TTo)instanceNew, key);
                    _factoriesInstances.Add(viewModelToName, key);
                }

                var instance = (ViewModelBase)SimpleIoc.Default.GetInstance(viewModelToName, key);
                if (instance != null)
                {
#if WINDOWS_PHONE
                    System.Uri instanceToNavigate;
#else
                    Type instanceToNavigate;
#endif
                    var result = _viewsRegister.TryGetValue(instance.GetType(), out instanceToNavigate);
                    if(result)
                    {
                        _currentViewModel = viewModelToName;
                        SetCurrentView(instanceToNavigate);
#if WINDOWS_PHONE
                        //Suppression de l'entr�e pr�c�dente si l'on ne doit pas navigu�e dessus
                        if (viewModelFromName == null)
                        {
                            try
                            {
                                if (_rootFrame != null)
                                {
                                    _rootFrame.RemoveBackEntry();
                                }
                            }
                            catch (Exception)
                            { }
                        }
#endif
                    }
                }

                //Gestion d'une fonction � appeler suite � la g�n�ration de l'instance
                if (!string.IsNullOrEmpty(functionToLoad))
                {
                    var method = typeof(TTo).GetMethod(functionToLoad, parametersFunction);
                    if (method != null)
                    {
                        method.Invoke(instance, parametersFunction);
                    }
                    else
                    {
                        var countParameters = parametersFunction != null
                                                  ? ((IEnumerable<object>) parametersFunction).Count()
                                                  : 0;
                        throw new FunctionToLoadNavigationException(string.Format("{0} with {1} parameter(s)", functionToLoad, countParameters), typeof (TTo).Name);
                    }
                }

                return key;
            }
            catch (Exception e)
            {
                throw new NavigationException(e);
            }
        }

#if WINDOWS_PHONE

        /// <summary>
        /// Affecte la propri�t� de view courante du ViewModel principal
        /// </summary>
        /// <param name="instanceToNavigate">
        /// Instance de la vue qui est devenue la vue courante
        /// </param>
        private void SetCurrentView(System.Uri instanceToNavigate)
        {
            try
            {
                _rootFrame.Navigate(instanceToNavigate);
            }
            catch (Exception e)
            {
                throw new FrameNavigationException();
            }
        }
#else
        /// <summary>
        /// Affecte la propri�t� de view courante du ViewModel principal
        /// </summary>
        /// <param name="instanceToNavigate">
        /// Instance de la vue qui est devenue la vue courante
        /// </param>
        private void SetCurrentView(Type instanceToNavigate)
        {
            try
            {
                _rootFrame.Navigate(instanceToNavigate);    
            }
            catch (Exception e)
            {
                throw new FrameNavigationException();
            }
        }
#endif

        /// <summary>
        /// Permet de g�rer l'�tat de navigation � l'instant T pour un ViewModel
        /// </summary>
        /// <param name="viewModelFromName">ViewModel pris en compte</param>
        private void SetNavigationHistory(Type viewModelFromName)
        {
            if (!_historyNavigation.ContainsKey(viewModelFromName))
            {
#if WINDOWS_PHONE
                _historyNavigation.Add(viewModelFromName, _rootFrame.CurrentSource);
#else
                _historyNavigation.Add(viewModelFromName, _rootFrame.GetNavigationState());
#endif
            }
        }

        /// <summary>
        /// Permet de revenir en arri�re dans la pile de navigation des pages
        /// </summary>
        private bool SetGoBack(Type historyViewModel)
        {
            //Sauvegarde de l'�tat actuel pour revenir en arri�re si il le faut
#if WINDOWS_PHONE
            var navigationSave = _rootFrame.CurrentSource;
            System.Uri navigationState;
#else
            var navigationSave = _rootFrame.GetNavigationState();
            string navigationState;
#endif

            //Chargement de l'�tat voulu
            if (_historyNavigation.ContainsKey(historyViewModel))
            {
                if (_historyNavigation.TryGetValue(historyViewModel, out navigationState))
                {
#if WINDOWS_PHONE
                    _rootFrame.GoBack(); 
#else
                    _rootFrame.SetNavigationState(navigationState);  
#endif
                }

#if WINDOWS_PHONE
                //TODO: Voir comment assurer une v�rification de la coh�rence de page
#else
                //V�rification de la bonne coh�rence
                Type historyType;
                var currentType = _rootFrame.CurrentSourcePageType;
                var result = _viewsRegister.TryGetValue(historyViewModel, out historyType);
                if (result)
                {
                    if (currentType != historyType)
                    {
                        //On reprend la position pr�c�dente car le type de page dans la Frame
                        //ne correspond pas � celui vers lequel on doit revenir
                        _rootFrame.SetNavigationState(navigationSave);
                        return false;
                    }
                }
#endif
            }
            return true;
        }

        /// <summary>
        /// Permet de savoir si l'on peut revenir en arriere au niveau des Frame
        /// </summary>
        /// <returns>R�sultat de la demande</returns>
        private bool CanGoBackFrame()
        {
            return _rootFrame.CanGoBack;
        }

        /// <summary>
        /// Vide l'historique de navigation de la classe et de la Frame de WinRT
        /// </summary>
        private void ClearNavigation()
        {
            _historyInstances.Clear();

            //On vide les instances dans SimpleIoc
            foreach (var instance in _factoriesInstances)
            {
                var instanceSimple = SimpleIoc.Default.GetInstance(instance.Key, instance.Value);
                SimpleIoc.Default.Unregister(instanceSimple);
            }
            _factoriesInstances.Clear();
#if WINDOWS_PHONE
            while (_rootFrame.RemoveBackEntry() != null)
            {}
#else
            _rootFrame.SetNavigationState(_navigationStateInitial);
#endif
        }

#if WINDOWS_PHONE_APP
        /// <summary>
        /// Fonction par d�faut du retour en arri�re par le bouton phyique ou virtuel du device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HardwareButtonsBackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            if (CanGoBack())
            {
                GoBack();
                e.Handled = true;
            }
        }
#endif

        #endregion
    }
}