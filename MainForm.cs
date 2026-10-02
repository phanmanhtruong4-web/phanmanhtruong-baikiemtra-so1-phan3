using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TechMartManager
{
    // ==========================================
    // 1. CÁC LỚP MODEL (DATA BINDING)
    // ==========================================
    public class Category
    {
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
    }

    public class Product
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryId { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ImagePath { get; set; }
    }

    // ==========================================
    // 2. LỚP GIAO DIỆN VÀ LOGIC CHÍNH
    // ==========================================
    public class MainForm : Form
    {
        // Khai báo Controls
        private TableLayoutPanel tlpMain;
        private Panel pnlInput;
        private DataGridView dgvProducts;
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatusCount;

        private TextBox txtProductId, txtProductName, txtUnitPrice, txtQuantity, txtSearch;
        private ComboBox cboCategory;
        private PictureBox picAvatar;
        private Button btnChooseImage, btnAdd, btnUpdate, btnDelete;
        private ErrorProvider errorProvider;

        // Data
        private BindingList<Product> _productList;
        private BindingSource _bindingSource;

        public MainForm()
        {
            // Thiết lập Form
            this.Text = "TechMart Product Manager";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            InitializeUI();   // Vẽ giao diện bằng code
            InitializeData(); // Nạp dữ liệu
        }

        private void InitializeUI()
        {
            // 1. MenuStrip & StatusStrip
            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var exportMenu = new ToolStripMenuItem("Export CSV (Ctrl+E)", null, MenuExportCSV_Click);
            var exitMenu = new ToolStripMenuItem("Exit (Ctrl+X)", null, (s, e) => Application.Exit());
            fileMenu.DropDownItems.Add(exportMenu);
            fileMenu.DropDownItems.Add(exitMenu);
            menuStrip.Items.Add(fileMenu);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            statusStrip = new StatusStrip();
            lblStatusCount = new ToolStripStatusLabel("Tổng số sản phẩm: 0");
            statusStrip.Items.Add(lblStatusCount);
            this.Controls.Add(statusStrip);

            // 2. TableLayoutPanel (TC01: Responsive Layout 35% - 65%)
            tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            
            // Panel chứa Khung nhập liệu (Cột trái)
            pnlInput = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            
            int y = 20;
            pnlInput.Controls.Add(new Label { Text = "Mã SP:", Location = new Point(10, y), AutoSize = true });
            txtProductId = new TextBox { Location = new Point(100, y), Width = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlInput.Controls.Add(txtProductId);

            y += 40;
            pnlInput.Controls.Add(new Label { Text = "Tên SP:", Location = new Point(10, y), AutoSize = true });
            txtProductName = new TextBox { Location = new Point(100, y), Width = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlInput.Controls.Add(txtProductName);

            y += 40;
            pnlInput.Controls.Add(new Label { Text = "Danh mục:", Location = new Point(10, y), AutoSize = true });
            cboCategory = new ComboBox { Location = new Point(100, y), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlInput.Controls.Add(cboCategory);

            y += 40;
            pnlInput.Controls.Add(new Label { Text = "Đơn giá:", Location = new Point(10, y), AutoSize = true });
            txtUnitPrice = new TextBox { Location = new Point(100, y), Width = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlInput.Controls.Add(txtUnitPrice);

            y += 40;
            pnlInput.Controls.Add(new Label { Text = "Số lượng:", Location = new Point(10, y), AutoSize = true });
            txtQuantity = new TextBox { Location = new Point(100, y), Width = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlInput.Controls.Add(txtQuantity);

            y += 40;
            picAvatar = new PictureBox { Location = new Point(100, y), Width = 100, Height = 100, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom };
            pnlInput.Controls.Add(picAvatar);
            btnChooseImage = new Button { Text = "Chọn Ảnh", Location = new Point(210, y), Width = 80 };
            btnChooseImage.Click += BtnChooseImage_Click; // TC04
            pnlInput.Controls.Add(btnChooseImage);

            y += 120;
            btnAdd = new Button { Text = "Thêm", Location = new Point(10, y), Width = 70 };
            btnAdd.Click += BtnAdd_Click;
            btnUpdate = new Button { Text = "Sửa", Location = new Point(90, y), Width = 70 };
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete = new Button { Text = "Xóa", Location = new Point(170, y), Width = 70 };
            btnDelete.Click += BtnDelete_Click;
            pnlInput.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete });

            // DataGridView (Cột phải)
            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            txtSearch = new TextBox { Dock = DockStyle.Top, Text = "Nhập tên SP để tìm kiếm..." };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            txtSearch.Click += (s, e) => { if(txtSearch.Text == "Nhập tên SP để tìm kiếm...") txtSearch.Clear(); };
            
            dgvProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                ReadOnly = true
            };
            dgvProducts.SelectionChanged += DgvProducts_SelectionChanged;

            // Thiết lập Cột cho DataGridView (TC03)
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductId", HeaderText = "Mã SP" });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProductName", HeaderText = "Tên SP", Width = 150 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CategoryId", HeaderText = "Danh Mục" });
            
            var priceCol = new DataGridViewTextBoxColumn { DataPropertyName = "UnitPrice", HeaderText = "Đơn Giá" };
            priceCol.DefaultCellStyle.Format = "N0"; // Format giá
            dgvProducts.Columns.Add(priceCol);
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "Số Lượng" });

            pnlRight.Controls.Add(dgvProducts);
            pnlRight.Controls.Add(txtSearch);

            // Gắn vào TableLayoutPanel
            tlpMain.Controls.Add(pnlInput, 0, 0);
            tlpMain.Controls.Add(pnlRight, 1, 0);
            
            this.Controls.Add(tlpMain);
            this.Controls.Add(statusStrip); // Mang status strip lên trên
            tlpMain.BringToFront();

            errorProvider = new ErrorProvider(this);
        }

        private void InitializeData()
        {
            var categories = new BindingList<Category>
            {
                new Category { CategoryId = "DT", CategoryName = "Điện thoại" },
                new Category { CategoryId = "LT", CategoryName = "Laptop" },
                new Category { CategoryId = "PK", CategoryName = "Phụ kiện" }
            };
            cboCategory.DataSource = categories;
            cboCategory.DisplayMember = "CategoryName";
            cboCategory.ValueMember = "CategoryId";

            _productList = new BindingList<Product>();
            _bindingSource = new BindingSource { DataSource = _productList };
            dgvProducts.DataSource = _bindingSource;
        }

        // TC04: Mở OpenFileDialog nạp ảnh
        private void BtnChooseImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    picAvatar.ImageLocation = ofd.FileName;
                }
            }
        }

        // TC02: Thêm mới kèm Validation
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            var p = new Product
            {
                ProductId = txtProductId.Text,
                ProductName = txtProductName.Text,
                CategoryId = cboCategory.SelectedValue.ToString(),
                UnitPrice = decimal.Parse(txtUnitPrice.Text),
                Quantity = int.Parse(txtQuantity.Text),
                ImagePath = picAvatar.ImageLocation
            };
            _productList.Add(p);
            UpdateStatus();
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null && ValidateInput())
            {
                var p = (Product)dgvProducts.CurrentRow.DataBoundItem;
                p.ProductId = txtProductId.Text;
                p.ProductName = txtProductName.Text;
                p.CategoryId = cboCategory.SelectedValue.ToString();
                p.UnitPrice = decimal.Parse(txtUnitPrice.Text);
                p.Quantity = int.Parse(txtQuantity.Text);
                p.ImagePath = picAvatar.ImageLocation;
                _bindingSource.ResetBindings(false);
            }
        }

        // TC05: Xóa có xác nhận Yes/No
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                if (MessageBox.Show("Xác nhận xóa?", "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _productList.Remove((Product)dgvProducts.CurrentRow.DataBoundItem);
                    UpdateStatus();
                }
            }
        }

        // Đẩy dữ liệu ngược lên Khung nhập liệu
        private void DgvProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null && dgvProducts.CurrentRow.DataBoundItem is Product p)
            {
                txtProductId.Text = p.ProductId;
                txtProductName.Text = p.ProductName;
                cboCategory.SelectedValue = p.CategoryId;
                txtUnitPrice.Text = p.UnitPrice.ToString();
                txtQuantity.Text = p.Quantity.ToString();
                picAvatar.ImageLocation = p.ImagePath;
            }
        }

        // Live Search
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.ToLower();
            if (keyword == "" || keyword == "nhập tên sp để tìm kiếm...")
                _bindingSource.DataSource = _productList;
            else
                _bindingSource.DataSource = new BindingList<Product>(_productList.Where(p => p.ProductName.ToLower().Contains(keyword)).ToList());
        }

        // Xuất CSV
        private void MenuExportCSV_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV File|*.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Mã SP,Tên SP,Danh Mục,Đơn Giá,Số Lượng");
                    foreach (var p in _productList)
                        sb.AppendLine($"{p.ProductId},{p.ProductName},{p.CategoryId},{p.UnitPrice},{p.Quantity}");
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Xuất CSV thành công!");
                }
            }
        }

        // TC02: Validation
        private bool ValidateInput()
        {
            errorProvider.Clear();
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                errorProvider.SetError(txtProductName, "Không được để trống");
                valid = false;
            }
            if (!decimal.TryParse(txtUnitPrice.Text, out decimal price) || price <= 0)
            {
                errorProvider.SetError(txtUnitPrice, "Giá phải > 0");
                valid = false;
            }
            if (!int.TryParse(txtQuantity.Text, out int qty) || qty < 0)
            {
                errorProvider.SetError(txtQuantity, "Số lượng >= 0");
                valid = false;
            }
            return valid;
        }

        private void UpdateStatus() => lblStatusCount.Text = $"Tổng số sản phẩm: {_productList.Count}";

        // Xử lý phím tắt Ctrl+E, Ctrl+X
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.E)) { MenuExportCSV_Click(null, null); return true; }
            if (keyData == (Keys.Control | Keys.X)) { Application.Exit(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
