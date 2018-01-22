﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using WeihanLi.Npoi;
using HorizontalAlignment = NPOI.SS.UserModel.HorizontalAlignment;

namespace DbTool
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// 数据库助手
        /// </summary>
        private DbHelper dbHelper;

        public MainForm()
        {
            InitializeComponent();
#if DEBUG
            txtConnString.Text = "server=.;database=AccountingApp;uid=liweihan;pwd=Admin888";
#endif
            lnkExcelTemplate.Links.Add(0, 2, "https://github.com/WeihanLi/DbTool/raw/master/DbTool/template.xls");
        }

        /// <summary>
        /// 连接数据库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtConnString.Text))
            {
                return;
            }
            try
            {
                dbHelper = new DbHelper(txtConnString.Text);
                var tables = dbHelper.GetTablesInfo();
                var tableList = (from table in tables orderby table.TableName select table).ToList();
                //
                cbTables.DataSource = tableList;
                cbTables.DisplayMember = "TableName";
                //
                lblConnStatus.Text = string.Format(Properties.Resources.ConnectSuccess, dbHelper.DatabaseName);
                btnGenerateModel0.Enabled = true;
                btnExportExcel.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("连接数据库失败," + ex.Message);
            }
        }

        /// <summary>
        /// 根据数据库表信息生成Model
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGenerateModel_Click(object sender, EventArgs e)
        {
            if (dbHelper == null)
            {
                MessageBox.Show("请先连接数据库");
                return;
            }
            if (cbTables.CheckedItems.Count <= 0)
            {
                MessageBox.Show("请先选择要生成model的表");
                return;
            }
            string prefix = txtPrefix.Text, suffix = txtSuffix.Text;
            var ns = txtNamespace.Text;
            var dialog = new FolderBrowserDialog
            {
                Description = "请选择要保存model的文件夹",
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var dir = dialog.SelectedPath;
                if (string.IsNullOrEmpty(ns))
                {
                    ns = "Models";
                }
                try
                {
                    var tableEntity = new TableEntity();
                    if (cbTables.CheckedItems.Count > 0)
                    {
                        foreach (var item in cbTables.CheckedItems)
                        {
                            if (item is TableEntity currentTable)
                            {
                                tableEntity.TableName = currentTable.TableName;
                                tableEntity.TableDescription = currentTable.TableDescription;
                                tableEntity.Columns = dbHelper.GetColumnsInfo(tableEntity.TableName);
                                var content = tableEntity.GenerateModelText(ns, prefix, suffix, cbGenField.Checked);
                                var path = dir + "\\" + tableEntity.TableName.TrimTableName() + ".cs";
                                File.WriteAllText(path, content, Encoding.UTF8);
                            }
                        }
                        MessageBox.Show("保存成功");
                        System.Diagnostics.Process.Start("Explorer.exe", dir);
                    }
                    else
                    {
                        MessageBox.Show("请选择要生成的表");
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show("IOException:" + ex.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 窗口大小变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Resize(object sender, EventArgs e)
        {
            tabControl1.Width = Size.Width;
            tabControl1.Height = Size.Height - 160;
            dataGridView.Width = Size.Width - 20;
            dataGridView.Height = tabControl1.Height * 2 / 5;
            txtGeneratedSqlText.Width = Size.Width - 20;
            txtGeneratedSqlText.Height = tabControl1.Height * 2 / 5;
            txtGeneratedSqlText.Top = dataGridView.Height + dataGridView.Top + 10;
            cbTables.Height = tabControl1.Height - 100;
            treeViewTable.Width = Size.Width / 5;
            treeViewTable.Height = tabControl1.Height * 4 / 5;
            txtCodeModelSql.Left = treeViewTable.Width + treeViewTable.Left + 10;
            txtCodeModelSql.Width = Size.Width * 4 / 5;
            txtCodeModelSql.Height = tabControl1.Height * 4 / 5;
        }

        /// <summary>
        /// 根据表信息生成创建SQL表信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnGenerateSQL_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtTableName.Text) || string.IsNullOrEmpty(txtTableDesc.Text))
            {
                return;
            }
            if (dataGridView.Rows.Count > 1)
            {
                var tableInfo = new TableEntity()
                {
                    TableName = txtTableName.Text.Trim(),
                    TableDescription = txtTableDesc.Text.Trim(),
                    Columns = new List<ColumnEntity>()
                };
                ColumnEntity column;
                for (var k = 0; k < dataGridView.Rows.Count - 1; k++)
                {
                    column = new ColumnEntity
                    {
                        ColumnName = dataGridView.Rows[k].Cells[0].Value.ToString(),
                        ColumnDescription = dataGridView.Rows[k].Cells[1].Value.ToString(),
                        IsPrimaryKey = dataGridView.Rows[k].Cells[2].Value != null && (bool)dataGridView.Rows[k].Cells[2].Value,
                        IsNullable = dataGridView.Rows[k].Cells[3].Value != null && (bool)dataGridView.Rows[k].Cells[3].Value,
                        DataType = dataGridView.Rows[k].Cells[4].Value.ToString(),
                        Size = dataGridView.Rows[k].Cells[5].Value == null ? 0 : Convert.ToInt32(dataGridView.Rows[k].Cells[5].Value.ToString()),
                        DefaultValue = dataGridView.Rows[k].Cells[6].Value
                    };
                    //
                    tableInfo.Columns.Add(column);
                }
                //sql
                var sql = tableInfo.GenerateSqlStatement(cbGenDbDescription.Checked);
                //注：创建数据表个人觉得属于危险操作，暂时先不考虑直接在数据库中生成表，可以将创建表的sql粘贴到所需执行的地方二次确认后再创建数据库表，如果确实要在数据库中直接生成表可以取消注释以下代码
                ////数据库连接字符串不为空则创建表
                //if (!String.IsNullOrEmpty(txtConnString.Text))
                //{
                //    new DbHelper(txtConnString.Text).ExecuteNonQuery(sql);
                //}
                txtGeneratedSqlText.Text = sql;
                Clipboard.SetText(sql);
                MessageBox.Show("生成成功,sql语句已赋值至粘贴板");
            }
        }

        /// <summary>
        /// 导入Excel生成创建数据库表sql与创建数据库表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnImportExcel_Click(object sender, EventArgs e)
        {
            var ofg = new OpenFileDialog
            {
                Multiselect = false,
                CheckFileExists = true,
                Filter = "Excel文件(*.xlsx)|*.xlsx|Excel97-2003(*.xls)|*.xls"
            };
            if (ofg.ShowDialog() == DialogResult.OK)
            {
                var path = ofg.FileName;
                var isNewFileVersion = path.EndsWith(".xlsx");
                var table = new TableEntity();
                Stream stream = null;
                try
                {
                    stream = File.OpenRead(path);
                    IWorkbook workbook;
                    if (isNewFileVersion)
                    {
                        workbook = new XSSFWorkbook(stream);
                    }
                    else
                    {
                        workbook = new HSSFWorkbook(stream);
                    }
                    dataGridView.Rows.Clear();
                    var tableCount = workbook.NumberOfSheets;
                    if (tableCount == 1)
                    {
                        var sheet = workbook.GetSheetAt(0);
                        table.TableName = sheet.SheetName.Trim();
                        var rows = sheet.GetRowEnumerator();
                        while (rows.MoveNext())
                        {
                            var row = (IRow)rows.Current;
                            if (row.RowNum == 0)
                            {
                                table.TableDescription = row.Cells[0].StringCellValue;
                                txtTableName.Text = table.TableName;
                                txtTableDesc.Text = table.TableDescription;
                                continue;
                            }
                            if (row.RowNum > 1)
                            {
                                var column = new ColumnEntity
                                {
                                    ColumnName = row.Cells[0].StringCellValue.Trim()
                                };
                                if (string.IsNullOrWhiteSpace(column.ColumnName))
                                {
                                    continue;
                                }
                                column.ColumnDescription = row.Cells[1].StringCellValue;
                                column.IsPrimaryKey = row.Cells[2].StringCellValue.Equals("Y");
                                column.IsNullable = row.Cells[3].StringCellValue.Equals("Y");
                                column.DataType = row.Cells[4].StringCellValue.ToUpper();
                                if (string.IsNullOrEmpty(row.Cells[5].ToString()))
                                {
                                    column.Size = Utility.GetDefaultSizeForDbType(column.DataType);
                                }
                                else
                                {
                                    column.Size = row.Cells[5].GetCellValue<int>();
                                }
                                if (row.Cells.Count > 6)
                                {
                                    column.DefaultValue = row.Cells[6].ToString();
                                }
                                table.Columns.Add(column);

                                var rowView = new DataGridViewRow();
                                rowView.CreateCells(
                                    dataGridView,
                                    column.ColumnName,
                                    column.ColumnDescription,
                                    column.IsPrimaryKey,
                                    column.IsNullable,
                                    column.DataType,
                                    column.Size,
                                    column.DefaultValue
                                );
                                dataGridView.Rows.Add(rowView);
                            }
                        }
                        //sql
                        var sql = table.GenerateSqlStatement(cbGenDbDescription.Checked);
                        //注：创建数据表个人觉得属于危险操作，暂时先不考虑直接在数据库中生成表，可以将创建表的sql粘贴到所需执行的地方二次确认后再创建数据库表，如果确实要在数据库中直接生成表可以取消注释以下代码
                        ////数据库连接字符串不为空则创建表
                        //if (!String.IsNullOrEmpty(txtConnString.Text))
                        //{
                        //    new DbHelper(txtConnString.Text).ExecuteNonQuery(sql);
                        //}
                        txtGeneratedSqlText.Text = sql;
                        Clipboard.SetText(sql);
                        MessageBox.Show("生成成功，sql语句已赋值至粘贴板");
                    }
                    else
                    {
                        var sbSqlText = new StringBuilder();
                        for (var i = 0; i < tableCount; i++)
                        {
                            table = new TableEntity();
                            var sheet = workbook.GetSheetAt(i);
                            table.TableName = sheet.SheetName;

                            sbSqlText.AppendLine();
                            var rows = sheet.GetRowEnumerator();
                            while (rows.MoveNext())
                            {
                                var row = (IRow)rows.Current;
                                if (row.RowNum == 0)
                                {
                                    table.TableDescription = row.Cells[0].StringCellValue;
                                    continue;
                                }
                                if (row.RowNum > 1)
                                {
                                    var column = new ColumnEntity
                                    {
                                        ColumnName = row.Cells[0].StringCellValue
                                    };
                                    if (string.IsNullOrWhiteSpace(column.ColumnName))
                                    {
                                        continue;
                                    }
                                    column.ColumnDescription = row.Cells[1].StringCellValue;
                                    column.IsPrimaryKey = row.Cells[2].StringCellValue.Equals("Y");
                                    column.IsNullable = row.Cells[3].StringCellValue.Equals("Y");
                                    column.DataType = row.Cells[4].StringCellValue;
                                    if (string.IsNullOrEmpty(row.Cells[5].ToString()))
                                    {
                                        column.Size = Utility.GetDefaultSizeForDbType(column.DataType);
                                    }
                                    else
                                    {
                                        column.Size = Convert.ToInt32(row.Cells[5].ToString());
                                    }
                                    if (row.Cells.Count > 6)
                                    {
                                        column.DefaultValue = row.Cells[6].ToString();
                                    }
                                    table.Columns.Add(column);
                                }
                            }
                            sbSqlText.AppendLine(table.GenerateSqlStatement(cbGenDbDescription.Checked));
                        }
                        var dialog = new FolderBrowserDialog
                        {
                            Description = "请选择要保存sql文件的文件夹",
                            ShowNewFolderButton = true
                        };
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            var dir = dialog.SelectedPath;
                            //获取文件名
                            var fileName = Path.GetFileNameWithoutExtension(path);
                            //
                            var sqlFilePath = dir + "\\" + fileName + ".sql";
                            File.WriteAllText(sqlFilePath, sbSqlText.ToString(), Encoding.UTF8);
                            MessageBox.Show("保存成功");
                            System.Diagnostics.Process.Start("Explorer.exe", dir);
                        }
                        else
                        {
                            Clipboard.SetText(sbSqlText.ToString());
                            MessageBox.Show("取消保存文件，sql语句已赋值至粘贴板");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    stream?.Dispose();
                }
            }
        }

        /// <summary>
        /// 导出数据库表信息到Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExportExcel_Click(object sender, EventArgs e)
        {
            if (dbHelper == null)
            {
                MessageBox.Show("请先连接数据库");
                return;
            }
            if (cbTables.CheckedItems.Count <= 0)
            {
                MessageBox.Show("请先选择要生成model的表");
                return;
            }
            var dialog = new FolderBrowserDialog
            {
                Description = "请选择要保存excel文件的文件夹",
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var dir = dialog.SelectedPath;
                try
                {
                    var tableEntity = new TableEntity();
                    if (cbTables.CheckedItems.Count > 0)
                    {
                        var tempFileName = cbTables.CheckedItems.Count > 1 ? dbHelper.DatabaseName : (cbTables.CheckedItems[0] as TableEntity)?.TableName;
                        var path = dir + "\\" + tempFileName + ".xlsx";
                        var workbook = ExcelHelper.PrepareWorkbook(path);
                        foreach (var item in cbTables.CheckedItems)
                        {
                            var currentTable = item as TableEntity;
                            if (currentTable == null)
                            {
                                continue;
                            }
                            tableEntity.TableName = currentTable.TableName;
                            tableEntity.TableDescription = currentTable.TableDescription;
                            tableEntity.Columns = dbHelper.GetColumnsInfo(tableEntity.TableName);
                            //Create Sheet
                            var tempSheet = workbook.CreateSheet(tableEntity.TableName);
                            //create title
                            var titleRow = tempSheet.CreateRow(0);
                            var titleCell = titleRow.CreateCell(0);
                            titleCell.SetCellValue(tableEntity.TableDescription);
                            var titleStyle = workbook.CreateCellStyle();
                            titleStyle.Alignment = HorizontalAlignment.Left;
                            var font = workbook.CreateFont();
                            font.FontHeight = 14 * 14;
                            font.FontName = "微软雅黑";
                            font.IsBold = true;
                            titleStyle.SetFont(font);
                            titleStyle.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Black.Index;
                            titleStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.SeaGreen.Index;
                            titleStyle.FillPattern = FillPattern.SolidForeground;
                            titleCell.CellStyle = titleStyle;
                            //merged cells on single row
                            //ATTENTION: don't use Region class, which is obsolete
                            tempSheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 6));

                            //create header
                            var headerRow = tempSheet.CreateRow(1);
                            var headerStyle = workbook.CreateCellStyle();
                            headerStyle.Alignment = HorizontalAlignment.Center;
                            var headerfont = workbook.CreateFont();
                            font.FontHeight = 11 * 11;
                            font.FontName = "微软雅黑";
                            titleStyle.SetFont(headerfont);

                            var headerCell0 = headerRow.CreateCell(0);
                            headerCell0.CellStyle = headerStyle;
                            headerCell0.SetCellValue("列名称");
                            var headerCell1 = headerRow.CreateCell(1);
                            headerCell1.CellStyle = headerStyle;
                            headerCell1.SetCellValue("列描述");
                            var headerCell2 = headerRow.CreateCell(2);
                            headerCell2.CellStyle = headerStyle;
                            headerCell2.SetCellValue("是否是主键");
                            var headerCell3 = headerRow.CreateCell(3);
                            headerCell3.CellStyle = headerStyle;
                            headerCell3.SetCellValue("是否可以为空");
                            var headerCell4 = headerRow.CreateCell(4);
                            headerCell4.CellStyle = headerStyle;
                            headerCell4.SetCellValue("数据类型");
                            var headerCell5 = headerRow.CreateCell(5);
                            headerCell5.CellStyle = headerStyle;
                            headerCell5.SetCellValue("数据长度");
                            var headerCell6 = headerRow.CreateCell(6);
                            headerCell6.CellStyle = headerStyle;
                            headerCell6.SetCellValue("默认值");

                            //exist any column
                            if (tableEntity.Columns.Any())
                            {
                                IRow tempRow;
                                ICell tempCell;
                                for (var i = 1; i <= tableEntity.Columns.Count; i++)
                                {
                                    tempRow = tempSheet.CreateRow(i + 1);
                                    tempCell = tempRow.CreateCell(0);
                                    tempCell.SetCellValue(tableEntity.Columns[i - 1].ColumnName);
                                    tempCell = tempRow.CreateCell(1);
                                    tempCell.SetCellValue(tableEntity.Columns[i - 1].ColumnDescription);
                                    tempCell = tempRow.CreateCell(2);
                                    tempCell.SetCellValue(tableEntity.Columns[i - 1].IsPrimaryKey ? "Y" : "N");
                                    tempCell = tempRow.CreateCell(3);
                                    tempCell.SetCellValue(tableEntity.Columns[i - 1].IsNullable ? "Y" : "N");
                                    tempCell = tempRow.CreateCell(4);
                                    tempCell.SetCellValue(tableEntity.Columns[i - 1].DataType.ToUpper());
                                    tempCell = tempRow.CreateCell(5);
                                    tempCell.SetCellValue(tableEntity.Columns[i - 1].Size > 0 ? tableEntity.Columns[i - 1].Size.ToString() : "");
                                    tempCell = tempRow.CreateCell(6);
                                    if (tableEntity.Columns[i - 1].DefaultValue != null)
                                    {
                                        tempCell.SetCellValue(tableEntity.Columns[i - 1].DefaultValue.ToString());
                                    }
                                    else
                                    {
                                        if (tableEntity.Columns[i - 1].DataType.ToUpper().Contains("INT") && tableEntity.Columns[i - 1].IsPrimaryKey)
                                        {
                                            tempCell.SetCellValue("IDENTITY(1,1)");
                                        }
                                    }
                                }
                            }

                            // 自动调整单元格的宽度
                            tempSheet.AutoSizeColumn(0);
                            tempSheet.AutoSizeColumn(1);
                            tempSheet.AutoSizeColumn(2);
                            tempSheet.AutoSizeColumn(3);
                            tempSheet.AutoSizeColumn(4);
                            tempSheet.AutoSizeColumn(5);
                            tempSheet.AutoSizeColumn(6);
                        }
                        workbook.WriteToFile(path);
                        MessageBox.Show("保存成功");
                        System.Diagnostics.Process.Start("Explorer.exe", dir);
                    }
                    else
                    {
                        MessageBox.Show("请选择要生成的表");
                    }
                }
                catch (IOException ex)
                {
                    MessageBox.Show("IOException:" + ex.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void lnkExcelTemplate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
        }

        private void btnImportModel_Click(object sender, EventArgs e)
        {
            var ofg = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = true,
                Filter = "C#文件(*.cs)|*.cs"
            };
            if (ofg.ShowDialog() == DialogResult.OK)
            {
                if (ofg.FileNames.Any(f => !f.EndsWith(".cs")))
                {
                    MessageBox.Show("不支持所选文件类型，只可以选择C#文件(*.cs)");
                    return;
                }

                try
                {
                    var tables = Utility.GeTableEntityFromSourceCode(ofg.FileNames);
                    if (tables == null)
                    {
                        MessageBox.Show("没有找到 Model");
                    }
                    else
                    {
                        txtCodeModelSql.Clear();
                        treeViewTable.Nodes.Clear();
                        foreach (var table in tables)
                        {
                            var node = treeViewTable.Nodes.Add(table.TableName);
                            node.ToolTipText = table.TableDescription ?? table.TableName;
                            node.Nodes.AddRange(table.Columns.Select(c => new TreeNode(c.ColumnName) { ToolTipText = c.ColumnDescription ?? c.ColumnName }).ToArray());
                            txtCodeModelSql.AppendText(table.GenerateSqlStatement(cbGenCodeSqlDescription.Checked));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString());
        }
    }
}