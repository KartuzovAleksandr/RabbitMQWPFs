using CommunityToolkit.Mvvm.ComponentModel;
using Models;
using System.Collections.ObjectModel;

namespace OrderProducerWPF
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<OrderMessage> orders = new();
        // тут тоже магия - автоматически будет Orders - далее он и используется

        partial void OnOrdersChanged(ObservableCollection<OrderMessage> value)
        {
            // Логика при изменении коллекции (опционально)
        }

        public void AddOrder(OrderMessage order)
        {
            Orders.Add(order);
        }

        public void LoadOrders(IEnumerable<OrderMessage> ordersList)
        {
            Orders.Clear();
            foreach (var order in ordersList)
                Orders.Add(order);
        }
    }
}
