using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class UnitSelection : MonoBehaviour {

    public GameObject formationPrefab;
    public GameObject unitPrefab;

    public bool selectionActive;

    private List<Unit> units;
    private List<Unit> unitsToDeselect;

    private Vector3 selectionStart;
    private Vector3 selectionEnd;

    private bool selectionBox;

    void Start() {
        units = new List<Unit>();
    }
    
    void Update() {
        if (selectionActive && Input.GetKeyDown(KeyCode.Escape)) {
            selectionActive = false;
            foreach (Unit unit in units) {
                if (!unit.formation || unit.formation.leader != unit)
                    unit.GetComponent<Renderer>().material.color = Color.white;
            }
            units.Clear();
        }

        if (Input.GetMouseButtonDown(0)) {
            selectionStart = Input.mousePosition;
            selectionBox = true;
        }

        if (unitsToDeselect != null && unitsToDeselect.Count > 0) {
            foreach (Unit unit in unitsToDeselect) {
                if (!unit.formation || unit.formation.leader != unit)
                    unit.GetComponent<Renderer>().material.color = Color.white;
                else
                    unit.GetComponent<Renderer>().material.color = Color.green;
            }
            unitsToDeselect = null;
        }

        if (selectionBox) {
            List<Unit> selectedUnits = GetUnitsInSelectionBox(selectionStart, Input.mousePosition);

            foreach (Unit unit in selectedUnits) {
                if (units.Contains(unit)) {
                    if (!unit.formation || unit.formation.leader != unit)
                        unit.GetComponent<Renderer>().material.color = Color.white;
                    else
                        unit.GetComponent<Renderer>().material.color = Color.green;
                } else {
                    unit.GetComponent<Renderer>().material.color = Color.red;
                }
            }

            unitsToDeselect = selectedUnits;
        }

        if (Input.GetMouseButtonUp(0)) {
            RaycastHit hit;
            selectionEnd = Input.mousePosition;
            selectionBox = false;
            unitsToDeselect = null;

            if (Vector3.Distance(selectionStart, selectionEnd) < 1.0f) {            
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100)) {
                    if (hit.collider.CompareTag("unit")) {
                        SelectUnit(hit.collider.GetComponent<Unit> ());
                    } else if (selectionActive) {
                        selectionActive = false;
                        if (units.Count > 1) {
                            foreach (Unit unit in units) {
                                unit.GetComponent<NavMeshAgent>().obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

                                if (unit.formation && unit.formation.leader == unit) {
                                    Debug.Log("broke formation");
                                    unit.formation.BreakFormation ();
                                } else if (unit.formation) {
                                    unit.formation.RemoveUnit(unit);
                                }

                                if (!unit.formation || unit.formation.leader != unit)
                                    unit.GetComponent<Renderer>().material.color = Color.white;
                                else
                                    unit.GetComponent<Renderer>().material.color = Color.green;
                            }
                            CreateFormation(hit.point);
                        } else if (units.Count == 1){
                            Unit unit = units[0];

                            if (!unit.formation || unit.formation.leader != unit)
                                unit.GetComponent<Renderer>().material.color = Color.white;
                            else
                                unit.GetComponent<Renderer>().material.color = Color.green;

                            if (unit.formation && unit.formation.leader != unit) {
                                unit.formation.RemoveUnit(unit);
                            }
                            unit.GetComponent<NavMeshAgent> ().destination = GetClosestTarget(unit.GetComponent<NavMeshAgent>(), hit.point);
                        }
                    }
                }
            } else {
                List<Unit> selectedUnits = GetUnitsInSelectionBox (selectionStart, selectionEnd);

                foreach (Unit unit in selectedUnits) {
                    SelectUnit(unit);
                }
            }
        } else if (Input.GetMouseButtonDown(1)) {
            RaycastHit hit;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100)) {
                Instantiate(unitPrefab, hit.point, Quaternion.identity);
            }
        }
    }

    List<Unit> GetUnitsInSelectionBox(Vector3 selectionStart, Vector3 selectionEnd) {
        List<Unit> selectedUnits = new List<Unit>();

        float xmin = Mathf.Min(selectionStart.x, selectionEnd.x);
        float ymin = Mathf.Min(selectionStart.y, selectionEnd.y);
        float width = Mathf.Max(selectionStart.x, selectionEnd.x) - xmin;
        float height = Mathf.Max(selectionStart.y, selectionEnd.y) - ymin;
        Rect selectionRect = new Rect(xmin, ymin, width, height);

        GameObject[] csel = GameObject.FindGameObjectsWithTag("unit");
        for (int i = 0; i < csel.Length; i++) {
            Vector3 position = Camera.main.WorldToScreenPoint(new Vector3(csel[i].transform.position.x, csel[i].transform.position.y, csel[i].transform.position.z));

            //If the object falls inside the box set its state to selected so we can use it later
            if (selectionRect.Contains(position)) {
                selectedUnits.Add(csel[i].GetComponent<Unit>());
            }
        }

        return selectedUnits;
    }

    void SelectUnit(Unit unit) {
        if (!selectionActive) {
            units.Clear();
            selectionActive = true;
        }
        if (units.Contains(unit)) {
            units.Remove(unit);
            if (!unit.formation || unit.formation.leader != unit)
                unit.GetComponent<Renderer>().material.color = Color.white;
            else
                unit.GetComponent<Renderer>().material.color = Color.green;
        } else {
            units.Add(unit);   
            unit.GetComponent<Renderer>().material.color = Color.red;
        }
    }

    void CreateFormation(Vector3 target) {
        float minDistance = Mathf.Infinity;
        Unit closestUnit = null;
        foreach (Unit unit in units) {
            float dist = Vector3.Distance(unit.transform.position, target);
            if (dist < minDistance) {
                minDistance = dist;
                closestUnit = unit;
            }
        }

        Debug.Assert(closestUnit.formation == null);

        closestUnit.GetComponent<Renderer>().material.color = Color.green;

        NavMeshAgent agent = closestUnit.GetComponent<NavMeshAgent> ();
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        Vector3 closestTarget = GetClosestTarget(agent, target);

        Formation formation = Instantiate(formationPrefab).GetComponent<Formation> ();
        formation.transform.SetParent(closestUnit.transform);

        formation.SetUnitsAndTarget(closestUnit, units, closestTarget);
    }

    Vector3 GetClosestTarget(NavMeshAgent agent, Vector3 target) {
        NavMeshHit hit;
        bool blocked = NavMesh.Raycast(target, agent.transform.position, out hit, NavMesh.AllAreas);
        if (blocked) {
            Debug.Log("target blocked!");
        }
        return target;
    }
}
