using System;
using System.Collections.Generic;
using System.Linq;
using SAJApi.Models;

namespace SAJApi.Custom
{
  public class biDashTree
  {
    private List<biDash> _dashList;

    public biDashTree(List<biDash> dashList)
    {
      _dashList = dashList;
    }

    public List<treeModel> MakeBiTreeModel()
    {
      List<treeModel> nodes = new List<treeModel>();
      FindAndNullParents(_dashList);
      foreach (biDash dash in _dashList)
      {
        bool flag = IsEmptyFolder(dash, _dashList);
        if (dash.parentID == Guid.Empty && !flag)
          nodes.Add(AddTreeNode(dash));
        else if (!flag)
        {
          treeModel parentTreeNode = GetParentTreeNode(dash.parentID.ToString(), nodes);
          if (parentTreeNode != null)
          {
            if (parentTreeNode.childs == null)
              parentTreeNode.childs = new List<treeModel>();
            parentTreeNode.childs.Add(AddTreeNode(dash));
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
        treeModel parentTreeNode = GetParentTreeNode(parentId, node.childs);
        if (parentTreeNode != null)
          return parentTreeNode;
      }
      return null;
    }

    private bool IsEmptyFolder(biDash dash, List<biDash> resultList)
    {
      if (dash.type == 1)
        return resultList.All(r => r.parentID != dash.itemID);
      return false;
    }

    private treeModel AddTreeNode(biDash dash)
    {
      string str = dash.description ?? dash.name;
      return new treeModel
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
        if (!resultList.Any(r => r.itemID == biDash.parentID))
          biDash.parentID = Guid.Empty;
      }
    }
  }
}
