using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum FormationType {
    BoxFormation
}

public class Formation : MonoBehaviour {

    public GameObject spotPrefab;

    public GameObject unitPrefab;

    public Unit leader;

    public int spotCount;

    public float formationSpeed;

    public float defaultRelativeDistance;

    public float relativeDistance;

    public int catchUpEventsBeforeReAssigningSpots;

    private int lastSpotCount;

    private List<Spot> spots;

    private List<Unit> units;

    private float rotationOffset;

    private float formationHeightDisplacement;

    private int updatesWithCatchUpTrue;

	void Awake () {
        spots = new List<Spot>();
        units = new List<Unit>();
        relativeDistance = defaultRelativeDistance;
	}
	
	void Update () {
        /*
        if (spotCount != lastSpotCount) {
            foreach (Unit unit in units) {
                Destroy(unit.gameObject);
            }
            units.Clear();
            units.Add(leader);
            AddSpots();
            lastSpotCount = spotCount;
        }*/
    }

    void FixedUpdate () {
        FitFormation();
        //HoldStandForLeader();
        CatchUp();
    }
    /*
    void HoldStandForLeader() {
        if (target == null) return;
        Unit closestToTarget = leader;
        float minDistance = Vector3.Distance(leader.transform.position, target.transform.position);
        foreach (Unit unit in units) {
            float distance = Vector3.Distance(unit.transform.position, target.transform.position);
            if (distance < minDistance) {
                minDistance = distance;
                closestToTarget = unit;
            }
        }

        foreach (Unit unit in units) {
            unit.GetComponent<FollowTarget>().holdStand = closestToTarget != leader;
        }
        leader.GetComponent<FollowTarget> ().holdStand = false;
    }*/

    void CatchUp() {
        bool needsToCatchUp = false;
        foreach (Unit unit in units) {
            if (unit.spot != null) {
                float distance = Vector3.Distance(unit.transform.position, unit.spot.transform.position);
                if (distance > relativeDistance) {
                    needsToCatchUp = true;
                }
            }
        }

        if (updatesWithCatchUpTrue++ > catchUpEventsBeforeReAssigningSpots) {
            updatesWithCatchUpTrue = 0;
            ReAssignSpots ();
        } else {
            updatesWithCatchUpTrue = 0;
        }

        foreach (Unit unit in units) {
            if (unit.spot != null) {
                float distance = Vector3.Distance(unit.transform.position, unit.spot.transform.position);
                if (distance <= relativeDistance && needsToCatchUp) {
                    unit.GetComponent<NavMeshAgent> ().speed = formationSpeed / 2;
                } else {
                    unit.GetComponent<NavMeshAgent>().speed = formationSpeed;
                }
            }
        }
    }

    void FitFormation() {
        if (!ValidSpots()) {

            // push units closer
            relativeDistance -= 0.1f;
            if (relativeDistance < 1.1f) {
                relativeDistance = 1.1f;

                // rotate formation
                rotationOffset += 10f;
                if (rotationOffset > 360f) {
                    rotationOffset = 0;

                    // change formation
                    formationHeightDisplacement += 0.1f;
                    if (formationHeightDisplacement > 1.0f) {
                        formationHeightDisplacement = 1.0f;
                    } else {
                        if (CanUpdateFormation()) {
                            UpdateSpots(true);
                        }
                    }
                }

                if (CanUpdateFormation()) {
                    UpdateSpots(true);
                }
            } else {
                if (CanUpdateFormation()) {
                    UpdateSpots(false);
                }
            }
        } else if (relativeDistance < defaultRelativeDistance) {
            relativeDistance += 0.1f;
            if (!CanUpdateFormation()) {
                relativeDistance -= 0.1f;
            } else {
                UpdateSpots(false);
            }

            if (Mathf.Abs(rotationOffset - 0.0f) < Mathf.Epsilon) {
                float originalRotation = rotationOffset;
                rotationOffset += 10.0f * (Mathf.Abs(rotationOffset) / rotationOffset);
                if (!CanUpdateFormation()) {
                    rotationOffset = originalRotation;
                } else {
                    UpdateSpots(true);
                }
            }

            if (formationHeightDisplacement > 0.0f) {
                float originalDisplacement = formationHeightDisplacement;
                formationHeightDisplacement -= 0.1f;
                if (formationHeightDisplacement < 0) {
                    formationHeightDisplacement = 0;
                }

                if (!CanUpdateFormation()) {
                    formationHeightDisplacement = originalDisplacement;
                } else {
                    UpdateSpots(true);
                }
            }
        }
    }

    bool ValidSpots() {
        foreach (Spot spot in spots) {
            NavMeshPath path = new NavMeshPath();
            leader.GetComponent<NavMeshAgent>().CalculatePath(spot.transform.position, path);

            if (path.status != NavMeshPathStatus.PathComplete) {
                return false;
            }
        }
        return true;
    }

    bool CanUpdateFormation() {
        List<Vector3> relativeSpotPositions = CalculateSpotPositions(leader, FormationType.BoxFormation);
        foreach (Vector3 position in relativeSpotPositions) {
            NavMeshPath path = new NavMeshPath();
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z)) return false;
            leader.GetComponent<NavMeshAgent>().CalculatePath(position, path);

            if (path.status != NavMeshPathStatus.PathComplete) {
                return false;
            }
        }
        return true;
    }

    void UpdateSpots(bool shouldReAssign) {
        List<Vector3> relativeSpotPositions = CalculateSpotPositions(leader, FormationType.BoxFormation);
        for (int i = 0; i < relativeSpotPositions.Count; i++) {
            Vector3 position = relativeSpotPositions[i];
            spots[i].transform.position = position;
        }
        if (shouldReAssign) {
            ReAssignSpots();
        }
    }

    void AddSpots() {
        if (spotCount < 2) return;
        // remove previous spots
        foreach (Spot spot in spots) {
            Destroy(spot.gameObject);
        }
        spots.Clear();
        // calculate spots for other units in formation
        List<Vector3> relativeSpotPositions = CalculateSpotPositions(leader, FormationType.BoxFormation);
        foreach (Vector3 position in relativeSpotPositions) {
            AddSpotAtLocation(position);
        }

        // assign leader spot
        float minDistance = Mathf.Infinity;
        Spot closestSpot = spots[0];
        foreach (Spot spot in spots) {
            float dist = Vector3.Distance(leader.transform.position, spot.transform.position);
            if (dist < minDistance) {
                minDistance = dist;
                closestSpot = spot;
            }
        }

        AssignSpot (leader, closestSpot);

        // assign spots to other units
        //AssignSpots ();
        ReAssignSpots ();
    }

    void AddSpotAtLocation(Vector3 position) {
        Spot newSpot = Instantiate(spotPrefab, position, Quaternion.identity).GetComponent<Spot>();
        newSpot.formation = this;
        newSpot.occupier = null;
        newSpot.transform.SetParent(this.transform);
        spots.Add(newSpot);
    }

    List<Vector3> CalculateSpotPositions(Unit relativeTo, FormationType formationType) {
        List<Vector3> spotPositions = new List<Vector3>();

        switch (formationType) {
            case FormationType.BoxFormation:
                float sqrtSpotCount = Mathf.Sqrt(spotCount);
                int formationWidth = Mathf.FloorToInt(sqrtSpotCount);
                int formationHeight = Mathf.CeilToInt(sqrtSpotCount);

                if (formationHeightDisplacement > 0) {
                    int amountToDisplace = Mathf.FloorToInt(formationHeightDisplacement * formationWidth);
                    formationWidth -= amountToDisplace;
                    formationHeight += amountToDisplace;
                }

                if (formationWidth * formationHeight > spotCount) {
                    if (formationHeight > 1) {
                        formationHeight -= 1;   
                    } else {
                        formationWidth -= 1;
                    }
                    Debug.Assert(formationWidth * formationHeight > 0);
                }

                //float offsetHeight = spotCount % 2 == 0 ? 0 : relativeDistance;
                //float offsetWidth = spotCount % 2 == 0 ? 0 : ((formationWidth - 1.0f) * relativeDistance) / 2.0f;

                //bool addedLeaderSpot = false;

                //if (spotCount % 2 == 1) {
                //    spotPositions.Add(relativeTo.transform.position);
                //}

                for (int x = 0; x < formationWidth; x++) {
                    for (int y = 0; y < formationHeight; y++) {
                        Vector3 spotPosition = relativeTo.transform.position;
                        //spotPosition.x += offsetWidth;

                        spotPosition.x -= x * relativeDistance;
                        spotPosition.z -= y * relativeDistance;

                        spotPositions.Add(RotatePointAroundPivot(spotPosition, relativeTo.transform.position, Vector3.up * rotationOffset));
                    }
                }

                int spotsToAdd = spotCount - (formationWidth * formationHeight);

                //if (spotCount % 2 == 1) {
                //    spotsToAdd--;
                //}

                if (spotsToAdd > 0) {
                    for (int x = 0; x < spotsToAdd; x++) {
                        Vector3 spotPosition = relativeTo.transform.position;
                        //spotPosition.x += offsetWidth;

                        spotPosition.x -= x * relativeDistance;
                        spotPosition.z -= formationHeight * relativeDistance;

                        spotPositions.Add(RotatePointAroundPivot(spotPosition, relativeTo.transform.position, Vector3.up * rotationOffset));
                    }
                }

                break;
        }

        Debug.Assert(spotPositions.Count == spotCount);

        return spotPositions;
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles) {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }
    /*
    void AssignSpots () {
        foreach (Spot spot in spots) {
            if (spot.occupier == null) {
                Unit newUnit = Instantiate(unitPrefab, spot.transform.position, leader.transform.rotation).GetComponent<Unit> ();
                units.Add(newUnit);
                AssignSpot(newUnit, spot);
            }
        }
    }*/

    void AssignSpot (Unit occupier, Spot spot) {
        spot.occupier = occupier;
        occupier.spot = spot;
        if (occupier != leader) {
            occupier.GetComponent<FollowTarget> ().target = spot.gameObject;
        }
    }

    void ReAssignSpots () {
        foreach (Spot spot in spots) {
            if (spot.occupier && spot.occupier != leader) {
                spot.occupier.spot = null;
                spot.occupier = null;
            }
        }

        foreach (Spot spot in spots) {
            if (spot.occupier == null) {
                float minDistance = Mathf.Infinity;
                Unit closestUnit = null;
                foreach (Unit unit in units) {
                    if (unit.spot != null) continue;

                    float dist = Vector3.Distance(unit.transform.position, spot.transform.position);
                    if (dist < minDistance) {
                        minDistance = dist;
                        closestUnit = unit;
                    }
                }

                AssignSpot(closestUnit, spot);
            }
        }
    }

    public void RemoveUnit (Unit rogueUnit) {
        rogueUnit.spot = null;
        rogueUnit.formation = null;
        rogueUnit.GetComponent<FollowTarget>().target = null;
        this.units.Remove(rogueUnit);
        foreach (Unit unit in units) {
            unit.GetComponent<FollowTarget>().target = null;
            unit.spot = null;
        }
        spotCount = units.Count;
        if (spotCount == 1) {
            leader.GetComponent<Renderer>().material.color = Color.white;
            BreakFormation ();
        } else {
            AddSpots();
        }
    }

    public void SetUnitsAndTarget(Unit leader, List<Unit> units, Vector3 target) {
        this.leader = leader;
        this.units = new List<Unit>(units);
        foreach (Unit unit in units) {
            unit.GetComponent<FollowTarget>().target = null;
            unit.spot = null;
            unit.formation = this;
        }
        this.leader.GetComponent<NavMeshAgent> ().destination = target;
        spotCount = units.Count;
        AddSpots();
    }

    public void BreakFormation () {
        foreach (Unit unit in units) {
            unit.GetComponent<FollowTarget> ().target = null;
            unit.spot = null;
            unit.formation = null;
        }
        Destroy(this.gameObject);
    }
}
