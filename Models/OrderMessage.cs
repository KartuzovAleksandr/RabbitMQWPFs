//Класс Должен быть объявлен как partial
//Поля Только private, без { get; set; }
//Имена свойств
//Генератор автоматически создаёт публичные свойства с именем в PascalCase
//(lowerCamel) - такое уже было у нас в ASP.net (но Kebab Case стиль там)
//(например, customerName → CustomerName)
// Сгенерировано автоматически
//public string? CustomerName
//{
//    get => customerName;
//    set => SetProperty(ref customerName, value);
//}
//Вычисляемые свойства
//Оставляйте как есть (без [ObservableProperty]), 
//но убедитесь, что они зависят от сгенерированных свойств (Price, Quantity)

using CommunityToolkit.Mvvm.ComponentModel;

namespace Models
{
    public partial class OrderMessage : ObservableObject // ⚠️ partial
    {
        [ObservableProperty]
        private string? customerName; // ✅ Приватное поле без { get; set; }

        [ObservableProperty]
        private string? productName;

        [ObservableProperty]
        private decimal price;

        [ObservableProperty]
        private int quantity;

        [ObservableProperty]
        private DateTime orderDate = DateTime.UtcNow; // Инициализация в поле

        // Вычисляемое свойство - к публичным
        public decimal TotalPrice => Price * Quantity;
    }
}