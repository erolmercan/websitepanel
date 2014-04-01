// Copyright (c) 2014, Outercurve Foundation.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must  retain  the  above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form  must  reproduce the  above  copyright  notice,
//   this list of conditions  and  the  following  disclaimer in  the documentation
//   and/or other materials provided with the distribution.
//
// - Neither  the  name  of  the  Outercurve Foundation  nor   the   names  of  its
//   contributors may be used to endorse or  promote  products  derived  from  this
//   software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING,  BUT  NOT  LIMITED TO, THE IMPLIED
// WARRANTIES  OF  MERCHANTABILITY   AND  FITNESS  FOR  A  PARTICULAR  PURPOSE  ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL,  SPECIAL,  EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO,  PROCUREMENT  OF  SUBSTITUTE  GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)  HOWEVER  CAUSED AND ON
// ANY  THEORY  OF  LIABILITY,  WHETHER  IN  CONTRACT,  STRICT  LIABILITY,  OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE)  ARISING  IN  ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

namespace WebsitePanel.Ecommerce.Portal.UserControls
{
	public partial class ProductHighlights : ecControlBase
	{
		public const string VIEW_STATE_KEY = "__ProductHighlights";

		public List<string> HighlightedItems
		{
			get
			{
				List<string> items = ViewState[VIEW_STATE_KEY] as List<string>;

				if (items == null)
				{
					items = new List<string>();
					ViewState[VIEW_STATE_KEY] = items;
				}

				return items;
			}
			set
			{
				// save items...
				ViewState[VIEW_STATE_KEY] = value;
				// bind displayed items
				BindHighlightedItems();
			}
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			gvProductHightlights.RowCommand += new GridViewCommandEventHandler(gvProductHightlights_RowCommand);
		}

		protected void btnAddHighlight_Click(object sender, EventArgs e)
		{
			// add item
			HighlightedItems.Add(txtHighlightText.Text.Trim());
			// cleanup textbox
			txtHighlightText.Text = String.Empty;
			// re-bind items
			BindHighlightedItems();
		}

		private void gvProductHightlights_RowCommand(object sender, GridViewCommandEventArgs e)
		{
			switch (e.CommandName)
			{
				case "ITEM_DELETE":
					DeleteHighlightedItem(Convert.ToInt32(e.CommandArgument));
						break;
				case "ITEM_MOVEDOWN":
					FlipHighlightedItems(Convert.ToInt32(e.CommandArgument), true);
					break;
				case "ITEM_MOVEUP":
					FlipHighlightedItems(Convert.ToInt32(e.CommandArgument), false);
					break;
			}
		}

		private void BindHighlightedItems()
		{
			gvProductHightlights.DataSource = HighlightedItems;
			gvProductHightlights.DataBind();
		}

		private void DeleteHighlightedItem(int itemIndex)
		{
			HighlightedItems.RemoveAt(itemIndex);

			BindHighlightedItems();
		}

		private void FlipHighlightedItems(int itemIndex, bool movedown)
		{
			// first item can't move up
			if (movedown && itemIndex == HighlightedItems.Count - 1)
				return;
			// last item can't move down
			if (!movedown && itemIndex == 0)
				return;
			// single item can't move in both directions
			if (HighlightedItems.Count == 1)
				return;
			// 
			string itemToFlip = HighlightedItems[itemIndex];
			// remove
			HighlightedItems.RemoveAt(itemIndex);
			// 
			if (movedown)
				HighlightedItems.Insert(itemIndex + 1, itemToFlip);
			else
				HighlightedItems.Insert(itemIndex - 1, itemToFlip);

			// re-bind item
			BindHighlightedItems();
		}
	}
}