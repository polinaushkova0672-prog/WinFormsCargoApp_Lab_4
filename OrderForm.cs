using System;
using System.Linq;
using System.Windows.Forms;
using WinFormsCargoApp.Logic;

namespace WinFormsCargoApp
{
    public class OrderForm : Form
    {
        private CargoCompany company;
        private Order editingOrder; // null when creating

        private ComboBox cbClient, cbTariff;
        private TextBox tbWeight;
        private Label lblPrice, lblFinal;
        private Button btnSave, btnCancel;

        public OrderForm(CargoCompany company)
        {
            this.company = company;
            InitializeComponents();
            Text = "Добавить заказ";
        }

        public OrderForm(CargoCompany company, Order order) : this(company)
        {
            editingOrder = order;
            Text = "Редактировать заказ";
            LoadOrderToForm(order);
        }

        private void InitializeComponents()
        {
            Width = 420; Height = 260; StartPosition = FormStartPosition.CenterParent;

            var lbl1 = new Label { Text = "Клиент:", Left = 10, Top = 20, Width = 80 };
            cbClient = new ComboBox { Left = 100, Top = 20, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };

            var lbl2 = new Label { Text = "Тариф:", Left = 10, Top = 60, Width = 80 };
            cbTariff = new ComboBox { Left = 100, Top = 60, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };

            var lbl3 = new Label { Text = "Вес (тонн):", Left = 10, Top = 100, Width = 80 };
            tbWeight = new TextBox { Left = 100, Top = 100, Width = 120 };
            tbWeight.TextChanged += TbWeight_TextChanged;

            lblPrice = new Label { Text = "Цена/тонну: -", Left = 10, Top = 140, Width = 200 };
            lblFinal = new Label { Text = "Итог: -", Left = 220, Top = 140, Width = 200 };

            btnSave = new Button { Text = "Сохранить", Left = 100, Top = 180, Width = 100 };
            btnCancel = new Button { Text = "Отмена", Left = 220, Top = 180, Width = 100 };

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { lbl1, cbClient, lbl2, cbTariff, lbl3, tbWeight, lblPrice, lblFinal, btnSave, btnCancel });

            LoadLists();
        }

        private void LoadLists()
        {
            cbClient.Items.Clear();
            foreach (var c in company.Clients) cbClient.Items.Add(c.Name);
            if (cbClient.Items.Count > 0) cbClient.SelectedIndex = 0;

            cbTariff.Items.Clear();
            foreach (var t in company.Tariffs) cbTariff.Items.Add(t.Name);
            if (cbTariff.Items.Count > 0) cbTariff.SelectedIndex = 0;

            UpdatePriceLabels();
        }

        private void LoadOrderToForm(Order order)
        {
            cbClient.SelectedItem = order.ClientName;
            cbTariff.SelectedItem = order.TariffName;
            tbWeight.Text = order.Weight.ToString();
            UpdatePriceLabels();
        }

        private void TbWeight_TextChanged(object? sender, EventArgs e) => UpdatePriceLabels();

        private void UpdatePriceLabels()
        {
            if (cbTariff.SelectedItem == null) return;
            var tName = cbTariff.SelectedItem.ToString();
            var tariff = company.Tariffs.FirstOrDefault(t => t.Name == tName);
            if (tariff == null) { lblPrice.Text = "Цена/тонну: -"; lblFinal.Text = "Итог: -"; return; }

            lblPrice.Text = $"Цена/тонну: {tariff.Price}";

            if (double.TryParse(tbWeight.Text, out double w))
            {
                var client = company.Clients.FirstOrDefault(c => c.Name == cbClient.SelectedItem?.ToString());
                double discount = client?.GetDiscountPercent() ?? 0;
                double final = Math.Round(tariff.Price * w * (1 - discount / 100.0), 2);
                lblFinal.Text = $"Итог: {final}";
            }
            else
            {
                lblFinal.Text = "Итог: -";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // validation
            if (cbClient.SelectedItem == null) { MessageBox.Show("Выберите клиента"); return; }
            if (cbTariff.SelectedItem == null) { MessageBox.Show("Выберите тариф"); return; }
            if (!double.TryParse(tbWeight.Text, out double weight) || weight <= 0) { MessageBox.Show("Неверный вес"); return; }

            string clientName = cbClient.SelectedItem.ToString()!;
            string tariffName = cbTariff.SelectedItem.ToString()!;
            var tariff = company.Tariffs.First(t => t.Name == tariffName);
            var client = company.Clients.First(c => c.Name == clientName);

            if (editingOrder == null)
            {
                // create
                var order = company.CreateOrder(clientName, tariffName, weight);
                if (order == null) { MessageBox.Show("Не удалось создать заказ"); return; }
            }
            else
            {
                // update
                editingOrder.ClientName = clientName;
                editingOrder.TariffName = tariffName;
                editingOrder.Weight = weight;
                editingOrder.PricePerTon = tariff.Price;
                editingOrder.DiscountPercent = client.GetDiscountPercent();
                editingOrder.CalculateFinal();
                company.UpdateOrder(editingOrder);
            }

            DialogResult = DialogResult.OK;
        }
    }
}
