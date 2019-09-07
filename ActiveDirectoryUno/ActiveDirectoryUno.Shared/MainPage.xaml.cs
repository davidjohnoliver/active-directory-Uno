using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ActiveDirectoryUno
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        async void OnSignInSignOut(object sender, RoutedEventArgs e)
        {
            AuthenticationResult authResult = null;
            IEnumerable<IAccount> accounts = await App.PCA.GetAccountsAsync();
            try
            {
                if (btnSignInSignOut.Content as string == "Sign in")
                {
                    // let's see if we have a user in our belly already
                    try
                    {
                        IAccount firstAccount = accounts.FirstOrDefault();
                        authResult = await App.PCA.AcquireTokenSilent(App.Scopes, firstAccount)
                                              .ExecuteAsync();
                        await RefreshUserDataAsync(authResult.AccessToken).ConfigureAwait(false);
                        Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { btnSignInSignOut.Content = "Sign out"; });
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        try
                        {
							authResult = await App.PCA.AcquireTokenInteractive(App.Scopes)
#if __ANDROID__
                                                      .WithParentActivityOrWindow(Uno.UI.ContextHelper.Current as Android.App.Activity)
#elif __IOS__
													  .WithParentActivityOrWindow(Window.RootViewController)
#endif
													  .ExecuteAsync();

                            await RefreshUserDataAsync(authResult.AccessToken);
                            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { btnSignInSignOut.Content = "Sign out"; });
                        }
                        catch (Exception ex2)
                        {

                        }
                    }
                }
                else
                {
                    while (accounts.Any())
                    {
                        await App.PCA.RemoveAsync(accounts.FirstOrDefault());
                        accounts = await App.PCA.GetAccountsAsync();
                    }

                    slUser.Visibility = Visibility.Collapsed;
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { btnSignInSignOut.Content = "Sign in"; });
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task RefreshUserDataAsync(string token)
        {
            //get data from API
            HttpClient client = new HttpClient();
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
            HttpResponseMessage response = await client.SendAsync(message);
            string responseString = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                JObject user = JObject.Parse(responseString);

                slUser.Visibility = Visibility.Visible;

                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {

                    lblDisplayName.Text = user["displayName"].ToString();
                    lblGivenName.Text = user["givenName"].ToString();
                    lblId.Text = user["id"].ToString();
                    lblSurname.Text = user["surname"].ToString();
                    lblUserPrincipalName.Text = user["userPrincipalName"].ToString();

                    // just in case
                    btnSignInSignOut.Content = "Sign out";
                });
            }
            else
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => (new ContentDialog { Title = "Something went wrong with the API call", Content = responseString, CloseButtonText = "Dismiss" }).ShowAsync());
            }
        }
    }
}
