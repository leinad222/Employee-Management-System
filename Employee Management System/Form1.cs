using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Employee_Management_System
{
    public partial class Form1 : Form
    {
        // repository and UI controls added for simple persistence and listing
        private IEmployeeRepository _repository;
        private System.Windows.Forms.DataGridView employeesDataGridView;
        private System.Windows.Forms.Button btnSaveEmployee;
        private System.Windows.Forms.Button btnRefreshEmployees;
        private System.Windows.Forms.Button btnDeleteEmployee;
        private System.Windows.Forms.Button btnConnectDb;
        private System.Windows.Forms.Button btnEditEmployee;
        private System.Windows.Forms.Label lblRepository;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.ComboBox cmbDesignationFilter;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
        private System.Windows.Forms.Label lblPageInfo;
        private int _pageSize = 10;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private List<Employee> _allEmployeesCache;
        private System.ComponentModel.BindingList<Employee> _bindingList;

        public Form1()
        {
            InitializeComponent();
        }

        private void BtnConnectDb_Click(object sender, EventArgs e)
        {
            try
            {
                var sqlRepo = new SqlEmployeeRepository();
                _repository = sqlRepo;
                RefreshGrid();
                MessageBox.Show("Connected to SQL LocalDB and switched repository.", "Database", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateRepositoryLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to SQL LocalDB: {ex.Message}\nUsing current repository.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateRepositoryLabel()
        {
            try
            {
                if (_repository is SqlEmployeeRepository)
                    lblRepository.Text = "Repository: SQL LocalDB";
                else if (_repository is FileEmployeeRepository)
                    lblRepository.Text = "Repository: File (employees.xml)";
                else
                    lblRepository.Text = "Repository: Unknown";
            }
            catch
            {
                // ignore
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // initialize repository (use SQL repository if available)
            try
            {
                _repository = new SqlEmployeeRepository();
            }
            catch
            {
                // fallback to file repository if LocalDB is not available
                _repository = new FileEmployeeRepository();
            }

            // make computed fields read-only
            try
            {
                CONVEYANCEtextBox.ReadOnly = true;
                MEDICALtextBox.ReadOnly = true;
                HOUSERENTtextBox.ReadOnly = true;
                GROSSPAYtextBox.ReadOnly = true;
                INCOMETAXtextBox.ReadOnly = true;
                NETSALARYtextBox.ReadOnly = true;
            }
            catch
            {
                // designer controls might not exist at design-time in some contexts; ignore failures
            }

            // create list UI controls at runtime
            employeesDataGridView = new System.Windows.Forms.DataGridView
            {
                Width = 760,
                Height = 240,
                Top = 10,
                Left = 10,
                ReadOnly = true,
                AutoGenerateColumns = false,
                SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            // make the grid dock to top so it's visible and resizes with the form
            employeesDataGridView.Dock = DockStyle.Top;
            employeesDataGridView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // define columns to display and bind to Employee properties
            employeesDataGridView.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
            {
                DataPropertyName = "Id",
                HeaderText = "ID",
                Width = 100
            });
            employeesDataGridView.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
            {
                DataPropertyName = "Name",
                HeaderText = "Name",
                Width = 200
            });
            employeesDataGridView.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
            {
                DataPropertyName = "Designation",
                HeaderText = "Designation",
                Width = 150
            });
            employeesDataGridView.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
            {
                DataPropertyName = "BasicPay",
                HeaderText = "Basic Pay",
                Width = 120,
                DefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle { Format = "N2" }
            });
            employeesDataGridView.Columns.Add(new System.Windows.Forms.DataGridViewTextBoxColumn
            {
                DataPropertyName = "NetSalary",
                HeaderText = "Net Salary",
                Width = 120,
                DefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle { Format = "N2" }
            });

            // search and filter controls above the grid
            txtSearch = new System.Windows.Forms.TextBox { Left = employeesDataGridView.Left, Top = employeesDataGridView.Bottom + 6, Width = 200 };
            cmbDesignationFilter = new System.Windows.Forms.ComboBox { Left = txtSearch.Right + 10, Top = txtSearch.Top, Width = 180, DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList };
            var lblSearch = new System.Windows.Forms.Label { Text = "Search (name/id):", Left = txtSearch.Left - 100, Top = txtSearch.Top + 3, Width = 100 };
            var lblFilter = new System.Windows.Forms.Label { Text = "Designation:", Left = cmbDesignationFilter.Left - 90, Top = cmbDesignationFilter.Top + 3, Width = 80 };

            // paging controls
            btnPrevPage = new System.Windows.Forms.Button { Text = "Prev", Left = cmbDesignationFilter.Right + 20, Top = txtSearch.Top, Width = 60 };
            btnNextPage = new System.Windows.Forms.Button { Text = "Next", Left = btnPrevPage.Right + 10, Top = txtSearch.Top, Width = 60 };
            lblPageInfo = new System.Windows.Forms.Label { Text = "Page 1/1", Left = btnNextPage.Right + 10, Top = txtSearch.Top + 6, Width = 100 };

            this.Controls.Add(lblSearch);
            this.Controls.Add(txtSearch);
            this.Controls.Add(lblFilter);
            this.Controls.Add(cmbDesignationFilter);
            this.Controls.Add(btnPrevPage);
            this.Controls.Add(btnNextPage);
            this.Controls.Add(lblPageInfo);

            txtSearch.TextChanged += (s, ev) => { _currentPage = 1; RefreshGrid(); };
            cmbDesignationFilter.SelectedIndexChanged += (s, ev) => { _currentPage = 1; RefreshGrid(); };
            btnPrevPage.Click += (s, ev) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(); } };
            btnNextPage.Click += (s, ev) => { if (_currentPage < _totalPages) { _currentPage++; RefreshGrid(); } };

            btnSaveEmployee = new System.Windows.Forms.Button { Text = "Save", Left = 580, Top = 10, Width = 100 };
            btnRefreshEmployees = new System.Windows.Forms.Button { Text = "Refresh", Left = 580, Top = 45, Width = 100 };
            btnEditEmployee = new System.Windows.Forms.Button { Text = "Edit", Left = 580, Top = 80, Width = 100 };
            btnDeleteEmployee = new System.Windows.Forms.Button { Text = "Delete", Left = 580, Top = 115, Width = 100 };
            btnConnectDb = new System.Windows.Forms.Button { Text = "Connect DB", Left = 580, Top = 150, Width = 100 };
            lblRepository = new System.Windows.Forms.Label { Text = "Repository: ", Left = 580, Top = 190, Width = 300 };

            this.Controls.Add(employeesDataGridView);
            // Ensure the grid is placed behind the form controls so designer controls appear on top
            employeesDataGridView.SendToBack();
            this.Controls.Add(btnSaveEmployee);
            this.Controls.Add(btnRefreshEmployees);
            this.Controls.Add(btnEditEmployee);
            this.Controls.Add(btnDeleteEmployee);
            this.Controls.Add(btnConnectDb);
            this.Controls.Add(lblRepository);

            btnSaveEmployee.Click += BtnSaveEmployee_Click;
            btnRefreshEmployees.Click += BtnRefreshEmployees_Click;
            btnDeleteEmployee.Click += BtnDeleteEmployee_Click;
            btnEditEmployee.Click += BtnEditEmployee_Click;
            btnConnectDb.Click += BtnConnectDb_Click;
            employeesDataGridView.SelectionChanged += EmployeesDataGridView_SelectionChanged;
            employeesDataGridView.CellDoubleClick += (s, ev) =>
            {
                if (ev.RowIndex >= 0)
                {
                    BtnEditEmployee_Click(s, EventArgs.Empty);
                }
            };

            // arrange designer controls to the right of the grid for a structured layout
            try
            {
                var baseX = employeesDataGridView.Right + 20;
                var y = 20;

                // Title
                label2.Left = baseX + 20;
                label2.Top = y;
                y += 80;

                // helper to position label + textbox pairs
                Action<Label, TextBox> place = (lbl, txt) =>
                {
                    lbl.Left = baseX;
                    lbl.Top = y;
                    txt.Left = baseX + 220;
                    txt.Top = y - 6;
                    txt.Width = 320;
                    y += 50;
                };

                place(label1, IDtextBox);
                place(label3, NAMEtextBox);
                place(label4, DESIGNATIONtextBox);
                place(label5, BASICPAYtextBox);
                place(label6, CONVEYANCEtextBox);
                place(label7, MEDICALtextBox);
                place(label8, HOUSERENTtextBox);
                place(label9, GROSSPAYtextBox);
                place(label10, INCOMETAXtextBox);
                place(label11, NETSALARYtextBox);

                // buttons
                btnSaveEmployee.Left = baseX;
                btnSaveEmployee.Top = y + 10;
                btnRefreshEmployees.Left = baseX + 110;
                btnRefreshEmployees.Top = y + 10;
                btnEditEmployee.Left = baseX + 220;
                btnEditEmployee.Top = y + 10;
                btnDeleteEmployee.Left = baseX;
                btnDeleteEmployee.Top = y + 60;
                btnConnectDb.Left = baseX + 110;
                btnConnectDb.Top = y + 60;
                lblRepository.Left = baseX;
                lblRepository.Top = y + 110;
            }
            catch
            {
                // ignore layout errors
            }

            RefreshGrid();
            UpdateRepositoryLabel();
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void IDtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(IDtextBox.Text))
            {
                 IDtextBox.Focus();
                 errorProvider.SetError(this.IDtextBox, "Please Enter Your ID");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void NAMEtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(NAMEtextBox.Text))
            {
                NAMEtextBox.Focus();
                errorProvider.SetError(this.NAMEtextBox, "Please Enter Your NAME");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void DESIGNATIONtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(DESIGNATIONtextBox.Text))
            {
                DESIGNATIONtextBox.Focus();
                errorProvider.SetError(this.DESIGNATIONtextBox, "Please Enter Your DESIGNATION");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void BASICPAYtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(BASICPAYtextBox.Text))
            {
                BASICPAYtextBox.Focus();
                errorProvider.SetError(this.BASICPAYtextBox, "Please Enter Your SALARY");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void NAMEtextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void CONVEYANCEtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CONVEYANCEtextBox.Text))
            {
                CONVEYANCEtextBox.Focus();
                errorProvider.SetError(this.CONVEYANCEtextBox, "Please Enter Your CONVEYANCE");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void DESIGNATIONtextBox_Validated(object sender, EventArgs e)
        {

        }

        private void MEDICALtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(MEDICALtextBox.Text))
            {
                MEDICALtextBox.Focus();
                errorProvider.SetError(this.MEDICALtextBox, "Please Enter Your MEDICAL CERTIFICATE");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void HOUSERENTtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(HOUSERENTtextBox.Text))
            {
                HOUSERENTtextBox.Focus();
                errorProvider.SetError(this.HOUSERENTtextBox, "Please Enter Your RENT");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void GROSSPAYtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(GROSSPAYtextBox.Text))
            {
                GROSSPAYtextBox.Focus();
                errorProvider.SetError(this.GROSSPAYtextBox, "Please Enter Your PAYROLL");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void INCOMETAXtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(INCOMETAXtextBox.Text))
            {
                INCOMETAXtextBox.Focus();
                errorProvider.SetError(this.INCOMETAXtextBox, "Please Provide Your  INCOME TAX");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void NETSALARYtextBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(NETSALARYtextBox.Text))
            {
                NETSALARYtextBox.Focus();
                errorProvider.SetError(this.NETSALARYtextBox, "Please Enter Your NET SALARY");
            }
            else
            {
                errorProvider.Clear();
            }
        }

        private void BASICPAYtextBox_TextChanged(object sender, EventArgs e)
        {
            // Parse using decimal for monetary values and delegate calculation to SalaryCalculator.
            if (!decimal.TryParse(BASICPAYtextBox.Text, out decimal basicPay))
            {
                // If input is not valid, clear computed fields.
                CONVEYANCEtextBox.Text = string.Empty;
                MEDICALtextBox.Text = string.Empty;
                HOUSERENTtextBox.Text = string.Empty;
                GROSSPAYtextBox.Text = string.Empty;
                INCOMETAXtextBox.Text = string.Empty;
                NETSALARYtextBox.Text = string.Empty;
                return;
            }

            var result = SalaryCalculator.Calculate(basicPay);

            CONVEYANCEtextBox.Text = result.Conveyance.ToString("0.##");
            MEDICALtextBox.Text = result.Medical.ToString("0.##");
            HOUSERENTtextBox.Text = result.HouseRent.ToString("0.##");
            GROSSPAYtextBox.Text = result.GrossPay.ToString("0.##");
            INCOMETAXtextBox.Text = result.IncomeTax.ToString("0.##");
            NETSALARYtextBox.Text = result.NetSalary.ToString("0.##");
        }

        // ... keep other methods as-is ...

        private void MEDICALtextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void GROSSPAYtextBox_TextChanged(object sender, EventArgs e)
        {

        }

        // Refresh the DataGridView from repository
        private void RefreshGrid()
        {
            try
            {
                // load all once and cache
                _allEmployeesCache = _repository.LoadAll() ?? new List<Employee>();

                // populate designation filter
                var designations = _allEmployeesCache.Select(e => e.Designation).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct().OrderBy(d => d).ToList();
                designations.Insert(0, "All");
                var prev = cmbDesignationFilter.SelectedItem as string;
                cmbDesignationFilter.SelectedIndexChanged -= (s, e) => { _currentPage = 1; RefreshGrid(); };
                cmbDesignationFilter.DataSource = designations;
                if (!string.IsNullOrEmpty(prev) && designations.Contains(prev))
                    cmbDesignationFilter.SelectedItem = prev;
                cmbDesignationFilter.SelectedIndexChanged += (s, e) => { _currentPage = 1; RefreshGrid(); };

                // Apply search & filter
                var query = _allEmployeesCache.AsEnumerable();
                var search = txtSearch?.Text?.Trim();
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(emp => (!string.IsNullOrEmpty(emp.Name) && emp.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                                                || (!string.IsNullOrEmpty(emp.Id) && emp.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
                }
                var selDesign = cmbDesignationFilter?.SelectedItem as string;
                if (!string.IsNullOrEmpty(selDesign) && selDesign != "All")
                {
                    query = query.Where(emp => string.Equals(emp.Designation, selDesign, StringComparison.OrdinalIgnoreCase));
                }

                var filtered = query.ToList();

                // paging
                _totalPages = Math.Max(1, (int)Math.Ceiling((double)filtered.Count / _pageSize));
                if (_currentPage < 1) _currentPage = 1;
                if (_currentPage > _totalPages) _currentPage = _totalPages;

                var pageData = filtered.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();

                var source = new System.Windows.Forms.BindingSource();
                source.DataSource = pageData;
                employeesDataGridView.DataSource = source;
                employeesDataGridView.Refresh();

                lblPageInfo.Text = $"Page {_currentPage}/{_totalPages} (Total: {filtered.Count})";

                // select first row if available
                if (employeesDataGridView.Rows.Count > 0)
                {
                    employeesDataGridView.ClearSelection();
                    employeesDataGridView.Rows[0].Selected = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load employees: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefreshEmployees_Click(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        private void BtnSaveEmployee_Click(object sender, EventArgs e)
        {
            // simple validation
            if (string.IsNullOrWhiteSpace(IDtextBox.Text))
            {
                MessageBox.Show("Please enter an ID.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                IDtextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(NAMEtextBox.Text))
            {
                MessageBox.Show("Please enter a name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                NAMEtextBox.Focus();
                return;
            }

            if (!decimal.TryParse(BASICPAYtextBox.Text, out decimal basicPay))
            {
                MessageBox.Show("Please enter a valid basic pay.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BASICPAYtextBox.Focus();
                return;
            }

            var calc = SalaryCalculator.Calculate(basicPay);

            var emp = new Employee
            {
                Id = IDtextBox.Text.Trim(),
                Name = NAMEtextBox.Text.Trim(),
                Designation = DESIGNATIONtextBox.Text.Trim(),
                BasicPay = basicPay,
                Conveyance = calc.Conveyance,
                Medical = calc.Medical,
                HouseRent = calc.HouseRent,
                GrossPay = calc.GrossPay,
                IncomeTax = calc.IncomeTax,
                NetSalary = calc.NetSalary
            };

            _repository.Save(emp);
            RefreshGrid();
            MessageBox.Show("Employee saved.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EmployeesDataGridView_SelectionChanged(object sender, EventArgs e)
        {
            // enable/disable buttons based on selection
            var hasSelection = employeesDataGridView.SelectedRows.Count > 0;
            btnDeleteEmployee.Enabled = hasSelection;
            btnEditEmployee.Enabled = hasSelection;

            // auto-load into form when selection changes
            if (hasSelection)
            {
                var row = employeesDataGridView.SelectedRows[0];
                if (row.DataBoundItem is Employee emp)
                {
                    IDtextBox.Text = emp.Id;
                    NAMEtextBox.Text = emp.Name;
                    DESIGNATIONtextBox.Text = emp.Designation;
                    BASICPAYtextBox.Text = emp.BasicPay.ToString("0.##");
                }
            }
        }

        private void BtnDeleteEmployee_Click(object sender, EventArgs e)
        {
            if (employeesDataGridView.SelectedRows.Count == 0)
                return;

            var row = employeesDataGridView.SelectedRows[0];
            if (row.DataBoundItem is Employee emp)
            {
                var r = MessageBox.Show($"Delete employee {emp.Name} ({emp.Id})?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.Yes)
                {
                    _repository.Delete(emp.Id);
                    RefreshGrid();
                }
            }
        }

        private void BtnEditEmployee_Click(object sender, EventArgs e)
        {
            if (employeesDataGridView.SelectedRows.Count == 0)
                return;

            var row = employeesDataGridView.SelectedRows[0];
            if (row.DataBoundItem is Employee emp)
            {
                // load into form for editing
                IDtextBox.Text = emp.Id;
                NAMEtextBox.Text = emp.Name;
                DESIGNATIONtextBox.Text = emp.Designation;
                BASICPAYtextBox.Text = emp.BasicPay.ToString("0.##");

                // computed fields will update via BASICPAYtextBox_TextChanged
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void T_TextChanged(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }
    }
}
