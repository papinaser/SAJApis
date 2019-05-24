using SAJApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAJApi.Custom
{
  public class biDashTree
  {
    private List<biDash> _dashList;

    public biDashTree(List<biDash> dashList)
    {
      this._dashList = dashList;
    }

    public List<treeModel> MakeBiTreeModel()
    {
      List<treeModel> nodes = new List<treeModel>();
      this.FindAndNullParents(this._dashList);
      foreach (biDash dash in this._dashList)
      {
        bool flag = this.IsEmptyFolder(dash, this._dashList);
        if (dash.parentID == Guid.Empty && !flag)
          nodes.Add(this.AddTreeNode(dash));
        else if (!flag)
        {
          treeModel parentTreeNode = this.GetParentTreeNode(dash.parentID.ToString(), nodes);
          if (parentTreeNode != null)
          {
            if (parentTreeNode.childs == null)
              parentTreeNode.childs = new List<treeModel>();
            parentTreeNode.childs.Add(this.AddTreeNode(dash));
          }
        }
      }
      return nodes;
    }

    private treeModel GetParentTreeNode(string parentId, List<treeModel> nodes)
    {
      foreach (treeModel node in nodes)
      {
        if (node.id == parentId)
          return node;
        treeModel parentTreeNode = this.GetParentTreeNode(parentId, node.childs);
        if (parentTreeNode != null)
          return parentTreeNode;
      }
      return (treeModel) null;
    }

    private bool IsEmptyFolder(biDash dash, List<biDash> resultList)
    {
      if (dash.type == 1)
        return resultList.All<biDash>((Func<biDash, bool>) (r => r.parentID != dash.itemID));
      return false;
    }

    private treeModel AddTreeNode(biDash dash)
    {
      string str = dash.description ?? dash.name;
      return new treeModel()
      {
        id = dash.itemID.ToString(),
        title = str
      };
    }

    private void FindAndNullParents(List<biDash> resultList)
    {
      foreach (biDash result in resultList)
      {
        biDash biDash = result;
        if (!resultList.Any<biDash>((Func<biDash, bool>) (r => r.itemID == biDash.parentID)))
          biDash.parentID = Guid.Empty;
      }
    }
  }
}
