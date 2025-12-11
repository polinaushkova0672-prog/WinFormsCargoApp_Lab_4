using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using WinFormsCargoApp.Logic;
using WinFormsCargoApp.Services;

namespace WinFormsCargoApp
{
  public class MainForm : Form
  {
    private CargoCompany company = new CargoCompany();
    private BindingList<Order> ordersBinding;
    private DataGridView dgv;
    private Button btnAdd, btnEdit, btnDelete, btnLoad, btnSave;

    // Для отслеживания текущей сортировки
    private string currentSortColumn = "";
    private SortDirection currentSortDirection = SortDirection.Ascending;

    private enum SortDirection { Ascending, Descending }

    public MainForm()
    {
      Text = "Cargo Company";
      Width = 1000;  // Увеличили ширину чтобы всё поместилось
      Height = 600;
      StartPosition = FormStartPosition.CenterScreen;

      InitializeDefaultData();
      InitializeComponents();
      BindData();
    }

    private void InitializeDefaultData()
    {
      company.InitializeDefaultTariffs();
      company.Clients.Add(new Client("Иван", ClientType.Постоянный));
      company.Clients.Add(new Client("Мария", ClientType.Постоянный));
      company.Clients.Add(new Client("Петр", ClientType.Обычный));
      company.Clients.Add(new Client("Анна", ClientType.Обычный));

      // Тестовые заказы
      company.CreateOrder("Иван", "Эконом", 5);
      company.CreateOrder("Мария", "Бизнес", 100);
      company.CreateOrder("Петр", "Стандарт", 25);
      company.CreateOrder("Анна", "Премиум", 300);
      company.CreateOrder("Иван", "Стандарт", 15);
      company.CreateOrder("Мария", "Эконом", 8);
    }

    private void InitializeComponents()
    {
      // ТАБЛИЦА - растягиваем на ВСЁ окно
      dgv = new DataGridView
      {
        Dock = DockStyle.Fill,  // ← ИЗМЕНИЛИ: Fill вместо Top
        AutoGenerateColumns = false,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false
      };
      dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
      dgv.MultiSelect = false;

      // Колонки таблицы
      dgv.Columns.Add(new DataGridViewTextBoxColumn
      {
        HeaderText = "№ заказа ▲",
        DataPropertyName = "Id",
        Width = 80
      });
      dgv.Columns.Add(new DataGridViewTextBoxColumn
      {
        HeaderText = "Клиент",
        DataPropertyName = "ClientName",
        Width = 120
      });
      dgv.Columns.Add(new DataGridViewTextBoxColumn
      {
        HeaderText = "Тариф",
        DataPropertyName = "TariffName",
        Width = 100
      });
      dgv.Columns.Add(new DataGridViewTextBoxColumn
      {
        HeaderText = "Вес (тонн)",
        DataPropertyName = "Weight",
        Width = 80
      });
      dgv.Columns.Add(new DataGridViewTextBoxColumn
      {
        HeaderText = "Цена/тонну",
        DataPropertyName = "PricePerTon",
        Width = 90
      });
      dgv.Columns.Add(new DataGridViewTextBoxColumn
      {
        HeaderText = "Скидка %",
        DataPropertyName = "DiscountPercent",
        Width = 70
      });
      dgv.Columns.Add(new DataGridViewTextBoxColumn
      {
        HeaderText = "Итог (руб)",
        DataPropertyName = "FinalPrice",
        Width = 100
      });

      // Обработчик клика на заголовки колонок
      dgv.ColumnHeaderMouseClick += Dgv_ColumnHeaderMouseClick;

      // ПАНЕЛЬ ДЛЯ КНОПОК - вверху
      var panel = new Panel
      {
        Dock = DockStyle.Top,
        Height = 50,  // Увеличили высоту панели
        BackColor = Color.LightGray  // Добавили фон для наглядности
      };

      // ВСЕ КНОПКИ - включая те что пропали
      btnAdd = new Button
      {
        Text = "Добавить заказ",
        Left = 10,
        Top = 10,
        Width = 120,
        Height = 30
      };
      btnEdit = new Button
      {
        Text = "Изменить",
        Left = 140,
        Top = 10,
        Width = 100,
        Height = 30
      };
      btnDelete = new Button
      {
        Text = "Удалить",
        Left = 250,
        Top = 10,
        Width = 100,
        Height = 30
      };
      btnLoad = new Button
      {
        Text = "Загрузить JSON",  // ← ВОТ ОНА!
        Left = 360,
        Top = 10,
        Width = 120,
        Height = 30
      };
      btnSave = new Button
      {
        Text = "Сохранить JSON",  // ← И ЭТА ТОЖЕ!
        Left = 490,
        Top = 10,
        Width = 120,
        Height = 30
      };

      // Обработчики событий для ВСЕХ кнопок
      btnAdd.Click += BtnAdd_Click;
      btnEdit.Click += BtnEdit_Click;
      btnDelete.Click += BtnDelete_Click;
      btnLoad.Click += BtnLoad_Click;
      btnSave.Click += BtnSave_Click;

      // Добавляем ВСЕ кнопки на панель
      panel.Controls.AddRange(new Control[] {
                btnAdd, btnEdit, btnDelete, btnLoad, btnSave
            });

      // Добавляем элементы на форму в правильном порядке
      Controls.Add(dgv);     // Сначала таблица (заполняет всё)
      Controls.Add(panel);   // Затем панель с кнопками (сверху)
    }

    private void Dgv_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
      string columnName = dgv.Columns[e.ColumnIndex].DataPropertyName;

      // Если кликнули на ту же колонку - меняем направление сортировки
      if (currentSortColumn == columnName)
      {
        currentSortDirection = currentSortDirection == SortDirection.Ascending
            ? SortDirection.Descending
            : SortDirection.Ascending;
      }
      else
      {
        currentSortColumn = columnName;
        currentSortDirection = SortDirection.Ascending;
      }

      // Сортируем данные
      SortData(columnName, currentSortDirection);

      // Обновляем заголовки с индикаторами сортировки
      UpdateColumnHeaders(columnName, currentSortDirection);
    }

    private void SortData(string columnName, SortDirection direction)
    {
      var sortedOrders = columnName switch
      {
        "Id" => direction == SortDirection.Ascending
            ? company.Orders.OrderBy(o => o.Id).ToList()
            : company.Orders.OrderByDescending(o => o.Id).ToList(),

        "ClientName" => direction == SortDirection.Ascending
            ? company.Orders.OrderBy(o => o.ClientName).ToList()
            : company.Orders.OrderByDescending(o => o.ClientName).ToList(),

        "TariffName" => direction == SortDirection.Ascending
            ? company.Orders.OrderBy(o => o.TariffName).ToList()
            : company.Orders.OrderByDescending(o => o.TariffName).ToList(),

        "Weight" => direction == SortDirection.Ascending
            ? company.Orders.OrderBy(o => o.Weight).ToList()
            : company.Orders.OrderByDescending(o => o.Weight).ToList(),

        "PricePerTon" => direction == SortDirection.Ascending
            ? company.Orders.OrderBy(o => o.PricePerTon).ToList()
            : company.Orders.OrderByDescending(o => o.PricePerTon).ToList(),

        "DiscountPercent" => direction == SortDirection.Ascending
            ? company.Orders.OrderBy(o => o.DiscountPercent).ToList()
            : company.Orders.OrderByDescending(o => o.DiscountPercent).ToList(),

        "FinalPrice" => direction == SortDirection.Ascending
            ? company.Orders.OrderBy(o => o.FinalPrice).ToList()
            : company.Orders.OrderByDescending(o => o.FinalPrice).ToList(),

        _ => company.Orders.ToList()
      };

      ordersBinding = new BindingList<Order>(sortedOrders);
      dgv.DataSource = ordersBinding;
    }

    private void UpdateColumnHeaders(string sortedColumn, SortDirection direction)
    {
      // Сначала убираем все индикаторы сортировки
      foreach (DataGridViewColumn column in dgv.Columns)
      {
        string baseHeader = column.HeaderText;
        if (baseHeader.EndsWith(" ▲") || baseHeader.EndsWith(" ▼"))
        {
          baseHeader = baseHeader.Substring(0, baseHeader.Length - 2);
        }
        column.HeaderText = baseHeader;
      }

      // Добавляем индикатор к текущей колонке
      var currentColumn = dgv.Columns.Cast<DataGridViewColumn>()
          .FirstOrDefault(c => c.DataPropertyName == sortedColumn);

      if (currentColumn != null)
      {
        string indicator = direction == SortDirection.Ascending ? " ▲" : " ▼";
        currentColumn.HeaderText = currentColumn.HeaderText + indicator;
      }
    }

    private void BindData()
    {
      ordersBinding = new BindingList<Order>(company.Orders);
      dgv.DataSource = ordersBinding;

      // Устанавливаем начальную сортировку по ID
      currentSortColumn = "Id";
      currentSortDirection = SortDirection.Ascending;
      UpdateColumnHeaders("Id", SortDirection.Ascending);
    }

    private void RefreshGrid()
    {
      // Сохраняем текущую сортировку при обновлении
      SortData(currentSortColumn, currentSortDirection);
      UpdateColumnHeaders(currentSortColumn, currentSortDirection);
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
      using var f = new OrderForm(company);
      if (f.ShowDialog() == DialogResult.OK)
      {
        RefreshGrid();
      }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
      if (dgv.CurrentRow == null) return;
      var order = dgv.CurrentRow.DataBoundItem as Order;
      if (order == null) return;

      using var f = new OrderForm(company, order);
      if (f.ShowDialog() == DialogResult.OK)
      {
        RefreshGrid();
      }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
      if (dgv.CurrentRow == null) return;
      var order = dgv.CurrentRow.DataBoundItem as Order;
      if (order == null) return;

      var res = MessageBox.Show("Удалить выбранный заказ?", "Подтвердите", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
      if (res == DialogResult.Yes)
      {
        company.DeleteOrder(order.Id);
        RefreshGrid();
      }
    }

    private void BtnLoad_Click(object? sender, EventArgs e)
    {
      using var ofd = new OpenFileDialog();
      ofd.Filter = "JSON files|*.json|All files|*.*";
      if (ofd.ShowDialog() == DialogResult.OK)
      {
        try
        {
          company = FileService.Load(ofd.FileName);
          RefreshGrid();
          MessageBox.Show("Данные загружены", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
          MessageBox.Show("Ошибка при загрузке: " + ex.Message);
        }
      }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
      using var sfd = new SaveFileDialog();
      sfd.Filter = "JSON files|*.json|All files|*.*";
      if (sfd.ShowDialog() == DialogResult.OK)
      {
        try
        {
          FileService.Save(sfd.FileName, company);
          MessageBox.Show("Данные сохранены", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
          MessageBox.Show("Ошибка при сохранении: " + ex.Message);
        }
      }
    }
  }
}