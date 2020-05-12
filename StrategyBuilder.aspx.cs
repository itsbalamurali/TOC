﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
//using TOC.Utility;

namespace TOC
{
    public partial class StrategyBuilder : System.Web.UI.Page
    {
        private static string NIFTY = "NIFTY";
        private static int NIFTY_COL_DIFF = 100;
        private static int NIFTY_LOT_SIZE = 75;

        private static string BANKNIFTY = "BANKNIFTY";
        private static int BANKNIFTY_COL_DIFF = 200;
        private static int BANKNIFTY_LOT_SIZE = 20;

        //Gridview columns
        private static int DELETE_COL_INDEX = 0;
        private static int CONTRACT_TYP_COL_INDEX = 1;
        private static int TRANSTYP_COL_INDEX = 2;
        private static int SP_COL_INDEX = 3;
        private static int PREMINUM_COL_INDEX = 4;
        private static int LOTS_COL_INDEX = 5;
        private static int SELECTED_SP_COL_INDEX = 16;
        private static int LOWER_SP_COL_START_INDEX = 6;
        private static int HIGHER_SP_COL_END_INDEX = 26;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                DataTable dataTable = AddColumns();
                AddBlankRows(dataTable, 5);
                GridviewDataBind(dataTable);
            }
        }

        private void AddBlankRows(DataTable dataTable, int iRowCount)
        {
            for (int iCount = 0; iCount < iRowCount; iCount++)
            {
                dataTable.Rows.Add(false, "CE", "BUY", "8500", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "");
            }
        }

        protected void rblOCType_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void GridviewDataBind(DataTable dataTable)
        {
            int avgOfSP = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                if (row["Strike Price"] != null && row["Strike Price"].ToString().Trim().Length > 0)
                    avgOfSP += Convert.ToInt32(row["Strike Price"]);
            }

            if (dataTable.Rows.Count > 0)
                avgOfSP = avgOfSP / dataTable.Rows.Count;

            int columnDiff = 0;
            if (rblOCType.SelectedValue.Equals(NIFTY))
                columnDiff = NIFTY_COL_DIFF;
            if (rblOCType.SelectedValue.Equals(BANKNIFTY))
                columnDiff = BANKNIFTY_COL_DIFF;


            //Assign header value to the center column
            dataTable.Columns[SELECTED_SP_COL_INDEX].ColumnName = avgOfSP.ToString();
            gvStrategy.Columns[SELECTED_SP_COL_INDEX].HeaderText = dataTable.Columns[SELECTED_SP_COL_INDEX].ColumnName;

            //Assign value to the left columns from the center
            for (int icount = SELECTED_SP_COL_INDEX - 1; icount >= LOWER_SP_COL_START_INDEX; icount--)
            {
                dataTable.Columns[icount].ColumnName = Math.Round(Convert.ToDouble(avgOfSP - (SELECTED_SP_COL_INDEX - icount) * columnDiff), 0).ToString();
                gvStrategy.Columns[icount].HeaderText = dataTable.Columns[icount].ColumnName;
            }

            //Assign value to the right columns from the center
            for (int icount = SELECTED_SP_COL_INDEX + 1; icount <= HIGHER_SP_COL_END_INDEX; icount++)
            {
                dataTable.Columns[icount].ColumnName = Math.Round(Convert.ToDouble(avgOfSP + (icount - SELECTED_SP_COL_INDEX) * columnDiff), 0).ToString();
                gvStrategy.Columns[icount].HeaderText = dataTable.Columns[icount].ColumnName;
            }

            gvStrategy.DataSource = dataTable;
            gvStrategy.DataBind();
        }

        protected double CalculateExpiryValue(string contractType, string transactionType, double strikePrice, double premiumPaid, double expiryPrice)
        {
            double result = 0;
            if (contractType == enumContractType.PE.ToString() && transactionType == enumTransactionType.BUY.ToString())
                result = FO.PutBuy(strikePrice, premiumPaid, expiryPrice);
            if (contractType == enumContractType.PE.ToString() && transactionType == enumTransactionType.SELL.ToString())
                result = FO.PutSell(strikePrice, premiumPaid, expiryPrice);
            if (contractType == enumContractType.CE.ToString() && transactionType == enumTransactionType.BUY.ToString())
                result = FO.CallBuy(strikePrice, premiumPaid, expiryPrice);
            if (contractType == enumContractType.CE.ToString() && transactionType == enumTransactionType.SELL.ToString())
                result = FO.CallSell(strikePrice, premiumPaid, expiryPrice);
            if (contractType == enumContractType.EQ.ToString() && transactionType == enumTransactionType.BUY.ToString())
                result = FO.EQBuy(strikePrice, expiryPrice);
            if (contractType == enumContractType.FUT.ToString() && transactionType == enumTransactionType.BUY.ToString())
                result = FO.FutBuy(strikePrice, expiryPrice);
            if (contractType == enumContractType.FUT.ToString() && transactionType == enumTransactionType.SELL.ToString())
                result = FO.FutSell(strikePrice, expiryPrice);

            return result;
        }

        protected void gvStrategy_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DropDownList ddlContractType = e.Row.FindControl("ddlContractType") as DropDownList;
                DropDownList ddlTransactionType = e.Row.FindControl("ddlTransactionType") as DropDownList;
                DropDownList ddlStrikePrice = e.Row.FindControl("ddlStrikePrice") as DropDownList;
                TextBox txtPremium = e.Row.FindControl("txtPremium") as TextBox;
                TextBox txtLots = e.Row.FindControl("txtLots") as TextBox;
                //Utility.SelectDataInCombo(ddlContractType,);

                //If blank then fill one lot
                if (txtPremium.Text.Trim().Length <= 0)
                {
                    txtPremium.Text = "1";
                }

                //If blank then fill one lot
                if (txtLots.Text.Trim().Length <= 0)
                {
                    txtLots.Text = "1";
                }

                int rowindex = e.Row.RowIndex + 1;
                ddlContractType.Attributes.Add("onchange", "CalcExpValForAllCellsInRow('" + gvStrategy.ClientID + "', '" + rowindex + "','" + rblOCType.ClientID + "');");
                ddlTransactionType.Attributes.Add("onchange", "CalcExpValForAllCellsInRow('" + gvStrategy.ClientID + "', '" + rowindex + "','" + rblOCType.ClientID + "');");
                ddlStrikePrice.Attributes.Add("onchange", "SetGridviewHeaderValues('" + gvStrategy.ClientID + "'), CalcExpValForAllCellsInRow('" + gvStrategy.ClientID + "', '" + rowindex + "','" + rblOCType.ClientID + "');");
                txtPremium.Attributes.Add("onchange", "CalcExpValForAllCellsInRow('" + gvStrategy.ClientID + "', '" + rowindex + "','" + rblOCType.ClientID + "');");

                int lotsSize = 0;
                if (rblOCType.SelectedValue.Equals(NIFTY))
                    lotsSize = NIFTY_LOT_SIZE;
                if (rblOCType.SelectedValue.Equals(BANKNIFTY))
                    lotsSize = BANKNIFTY_LOT_SIZE;



                for (int icount = LOWER_SP_COL_START_INDEX; icount <= HIGHER_SP_COL_END_INDEX; icount++)
                {
                    e.Row.Cells[icount].Text = Convert.ToString(Convert.ToInt32(txtLots.Text) * lotsSize * CalculateExpiryValue(ddlContractType.SelectedValue, ddlTransactionType.SelectedValue, Convert.ToDouble(ddlStrikePrice.SelectedValue), Convert.ToDouble(txtPremium.Text), Convert.ToDouble(gvStrategy.Columns[icount].HeaderText)));
                }
            }
        }

        private DataTable AddColumns()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Delete");
            dataTable.Columns.Add("Contract Type");
            dataTable.Columns.Add("Transaction Type");
            dataTable.Columns.Add("Strike Price");
            dataTable.Columns.Add("Lots");
            dataTable.Columns.Add("Premium");
            dataTable.Columns.Add("11");
            dataTable.Columns.Add("12");
            dataTable.Columns.Add("13");
            dataTable.Columns.Add("14");
            dataTable.Columns.Add("15");
            dataTable.Columns.Add("16");
            dataTable.Columns.Add("17");
            dataTable.Columns.Add("18");
            dataTable.Columns.Add("19");
            dataTable.Columns.Add("20");
            dataTable.Columns.Add("1");
            dataTable.Columns.Add("21");
            dataTable.Columns.Add("22");
            dataTable.Columns.Add("23");
            dataTable.Columns.Add("24");
            dataTable.Columns.Add("25");
            dataTable.Columns.Add("26");
            dataTable.Columns.Add("27");
            dataTable.Columns.Add("28");
            dataTable.Columns.Add("29");
            dataTable.Columns.Add("30");
            return dataTable;
        }

        protected void btnAddRows_Click(object sender, EventArgs e)
        {
            DataTable dataTable = SaveGridviewData();
            AddBlankRows(dataTable, 5);
            GridviewDataBind(dataTable);
        }

        private DataTable SaveGridviewData()
        {
            DataTable dataTable = AddColumns();

            foreach (GridViewRow gvRow in gvStrategy.Rows)
            {
                CheckBox chkDelete = gvRow.Cells[DELETE_COL_INDEX].FindControl("chkDelete") as CheckBox;
                DropDownList ddlContractType = gvRow.Cells[CONTRACT_TYP_COL_INDEX].FindControl("ddlContractType") as DropDownList;
                DropDownList ddlTransactionType = gvRow.Cells[TRANSTYP_COL_INDEX].FindControl("ddlTransactionType") as DropDownList;
                DropDownList ddlStrikePrice = gvRow.Cells[SP_COL_INDEX].FindControl("ddlStrikePrice") as DropDownList;
                TextBox txtPremium = gvRow.Cells[PREMINUM_COL_INDEX].FindControl("txtPremium") as TextBox;
                TextBox txtLots = gvRow.Cells[LOTS_COL_INDEX].FindControl("txtLots") as TextBox;

                dataTable.Rows.Add(chkDelete.Checked, ddlContractType.SelectedValue, ddlTransactionType.SelectedValue, ddlStrikePrice.SelectedValue, txtPremium.Text, txtLots.Text, "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "");

            }
            return dataTable;
        }
    }
}