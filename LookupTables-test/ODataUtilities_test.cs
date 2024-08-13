using Laserfiche.LookupTables.ODataApi;
// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace LookupTables_test;

[TestClass]
public class ODataUtilities_test
{
    [TestMethod]
    public void CreateODataApiScope_ReadWrite()
    {
        //Arrange


        //Act
        var scope = ODataUtilities.CreateODataApiScope(true, true, "project/Global");

        //Assert
        Assert.AreEqual("table.Read table.Write project/Global", scope);
    }

    [TestMethod]
    public void CreateODataApiScope_Read()
    {
        //Arrange


        //Act
        var scope = ODataUtilities.CreateODataApiScope(true, false, "project/Global");

        //Assert
        Assert.AreEqual("table.Read project/Global", scope);
    }

    [TestMethod]
    public void CreateODataApiScope_Write()
    {
        //Arrange


        //Act
        var scope = ODataUtilities.CreateODataApiScope(false, true, "");

        //Assert
        Assert.AreEqual("table.Write", scope);
    }
}