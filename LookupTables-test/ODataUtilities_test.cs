using Laserfiche.LookupTables.ODataApi;
using System.Text.Json;
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

    [TestMethod]
    public void JsonConvert_DeserializeObject_Completed_TaskProgress()
    {
        //Arrange
        var taskProgressJson = "{\"@odata.context\":\"https://api.a.clouddev.laserfiche.com/odata4/general/$metadata#Tasks/$entity\"," +
            "\"Id\":\"a3ab8f14-ac9d-4ee9-a19e-63309be3f957\"," +
            "\"Type\":\"ReplaceTable\"," +
            "\"Status\":\"Completed\"," +
            "\"StartTime\":\"2024-08-17T00:45:41.3602916Z\"," +
            "\"LastUpdateTime\":\"2024-08-17T00:45:41.4460822Z\"," +
            "\"Errors\":[]," +
            "\"Result\":{\"@odata.type\":\"#Laserfiche.OData.General.ReplaceTableOperationResult\",\"NumberOfRows\":4,\"TableName\":\"ALL_DATA_TYPES_TABLE_SAMPLE\"}}";

        //Act
        var taskProgress = System.Text.Json.JsonSerializer.Deserialize<TaskProgress>(taskProgressJson, JsonSerializerOptions.Default);

        //Assert
        Assert.AreEqual("a3ab8f14-ac9d-4ee9-a19e-63309be3f957", taskProgress?.Id);
        Assert.AreEqual(Laserfiche.LookupTables.ODataApi.TaskStatus.Completed, taskProgress?.Status);
        Assert.AreEqual(4, taskProgress?.Result.GetProperty("NumberOfRows").GetInt32());
        Assert.AreEqual(0, taskProgress?.Errors.Count);
    }
}