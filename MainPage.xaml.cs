using System.Collections.ObjectModel;

namespace Subscription_tracker;

public partial class MainPage : ContentPage
{
    // Si no quieres esto, puedes quitarlo; es solo ejemplo de frontend:
    private readonly ObservableCollection<object> _subscriptions =
        new();

    public MainPage()
    {
        InitializeComponent();

        // Para que el CollectionView no truene al iniciar
        SubscriptionsCollection.ItemsSource = _subscriptions;
    }

    // Toolbar y botón "Agregar suscripción"
    private void OnAddSubscriptionClicked(object sender, EventArgs e)
    {
        // TODO: Navegar a formulario o abrir modal
        // await Navigation.PushAsync(new SubscriptionFormPage());
    }

    // Búsqueda por texto
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // TODO: filtrar ItemsSource según e.NewTextValue
    }

    // Filtro por ciclo de facturación
    private void OnBillingCycleFilterChanged(object sender, EventArgs e)
    {
        // TODO: filtrar lista según BillingCyclePicker.SelectedItem
    }

    // Tap en una suscripción de la lista
    private void OnSubscriptionSelected(object sender, SelectionChangedEventArgs e)
    {
        // TODO: abrir detalle / formulario de edición
        // var selected = e.CurrentSelection.FirstOrDefault();
    }
}
