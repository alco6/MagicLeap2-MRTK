using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public class ReadCSV : MonoBehaviour
{
    public GameObject spherePrefab; // Prefab for the sphere
    public GameObject graphPrefab;
    public Material[] materials;

    void Start()
    {
        string path = Application.dataPath + "/sarpereAt.csv"; // path to the CSV file inside the Assets folder

        CreateGraph(path, 2);
        CreateGraph(path, 3);
        CreateGraph(path, 4);
        CreateGraph(path, 5);
    }

    public void CreateGraph(string dataPath, int rowNumber)
    {
        List<string[]> rows = new List<string[]>();
        Material material = materials[rowNumber - 2];

        GameObject graph = Instantiate(graphPrefab, new Vector3(0,0,1), Quaternion.identity);

        StreamReader reader = new StreamReader(dataPath);
        while (!reader.EndOfStream)
        {
            string line = reader.ReadLine();
            string[] row = line.Split(',');
            rows.Add(row);
            Debug.Log(row);
        }
        reader.Close();

        for (int i = 0; i < rows.Count; i++)
        {
            float value = float.Parse(rows[i][rowNumber]); // get the value from the second column
            float x = i; // use the index as the x position
            float y = value; // use the value as the y position

            Vector3 position = new Vector3(x, y, 0); // create a position vector
            GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity); // instantiate the sphere
            sphere.GetComponent<MeshRenderer>().material = material;
            sphere.transform.SetParent(graph.transform);
        }

        GenerateAxis("x", 190, graph);
        GenerateAxis("y", 40, graph);

        graph.transform.localScale = graph.transform.localScale / 100;
        graph.GetComponent<BoxCollider>().center = new Vector3(100, 25, 0);
        graph.GetComponent<BoxCollider>().size = new Vector3(200, 50, 0);
    }

    public void GenerateAxis(string direction, int size, GameObject graph)
    {
        Vector3 pos = new Vector3(0, 0, 0);

        for (int i = 0; i < size; i++)
        {
            GameObject axis = Instantiate(spherePrefab, pos, Quaternion.identity);

            axis.transform.SetParent(graph.transform);

            if(direction == "x")
            {
                pos += new Vector3(1, 0, 0);
            }
            if (direction == "y")
            {
                pos += new Vector3(0, 1, 0);
            }
        }
    }
}