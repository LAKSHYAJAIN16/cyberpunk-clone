using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PatrollingEnemy : MonoBehaviour
{
	//Movement vars
	public float speed = 5;
	public float waitTime = .3f;
	public float turnSpeed = 90;

	//Attack
	public float AttackDistance, attackCooldown = 0.25f;
	bool readyToAttack = true;

	//Sight
	public float viewDistance;
	public Light DetectionLight;

	//other
	private float viewAngle;
	private Color orgColor;

	//Player
	public Transform Player;

	//Bools and states
	private bool Patrolling = true;
	public State state;

	//Transform follow vars
	public Transform[] wayPoints;

	//Start function
	void Start()
	{
		//make an array
		Vector3[] points = new Vector3[wayPoints.Length];

        //Loop through and assign
        for (int i = 0; i < wayPoints.Length; i++)
        {
			points[i] = wayPoints[i].position;
        }

		//Set values
		viewAngle = DetectionLight.spotAngle;
		orgColor = DetectionLight.color;

		//Start coroutine
		StartCoroutine(FollowPath(points));
	}

	//Updtate function
	void Update()
	{
        if (PlayerInSight()){

			//Set color
			DetectionLight.color = Color.red;

			//Set state to chasing
			Patrolling = false;
        }

        else
        {
			Patrolling = true;
			DetectionLight.color = orgColor;
        }
    }

    /// <returns>
    /// Returns true if player is in sight range
    /// </returns>
	private bool PlayerInSight()
	{
		//Get view angle and view distance
		float viewAngleR = this.viewAngle;
		float viewDistanceR = this.viewDistance;

		//If we are chasing, increase view distance and view angle
		if (state == State.chasing){
			viewDistanceR = viewDistance * 10f;
			viewAngleR = viewAngle * 3f;
		}

		//Get distance
		float dist = GetDistance(Player.position, transform.position);

        //If distance is less than view distance, player in sight range
        if (dist <= viewDistance)
		{
			//Get dif vector
			Vector3 difV = Player.position - transform.position;

			//Normalize vector
			Vector3 dirToPlayer = difV.normalized;

			//Get angle
			float angle = Vector3.Angle(transform.forward, dirToPlayer);

			if (angle <= viewAngle){ 
				return true;
            }

			if (state == State.chasing){
				return true;
            }
        }

		return false;
    }

	/// <summary>
    /// Follow path Coroutine
    /// </summary>
	IEnumerator FollowPath(Vector3[] Points)
	{
		int targetPointIndex = 0;
		Vector3 TargetPoint = Points[targetPointIndex];

		while (true)
		{
			//Set state to walking			
			state = State.walking;

			//Some stufdf idduno
			float AttackMultiplier = 1f;
			
			//If not patrolling, chase
			if (!Patrolling)
			{
				//Turn to face enemy
				TurnToChase();

				//Calculate Distance
				float dist = GetDistance(Player.position, transform.position);

                //if distance is less than attacking distance, attack
                if (dist <= AttackDistance) {
					Attack();
					AttackMultiplier = 0.01f;
                }

				//else, move
				else if (dist > AttackDistance || !readyToAttack) {
					TargetPoint = new Vector3(Player.position.x, transform.position.y, Player.position.z);
					state = State.chasing;
				}
			}

			//Move torwards it
			transform.position = Vector3.MoveTowards(transform.position, TargetPoint, speed * Time.deltaTime * AttackMultiplier);

			//If reached, turn and head for next wayPoint
			if (transform.position == TargetPoint && Patrolling)
			{
				targetPointIndex = (targetPointIndex + 1) % Points.Length;
				TargetPoint = Points[targetPointIndex];
				state = State.turning;
				yield return new WaitForSeconds(waitTime);
				yield return StartCoroutine(TurnToFace(TargetPoint));
			}


			yield return null;
		}
	}

	/// <summary>
    /// Turns target to face dir
    /// </summary>
	IEnumerator TurnToFace(Vector3 lookTarget)
	{
		Vector3 dirToLookTarget = (lookTarget - transform.position).normalized;
		float targetAngle = 90 - Mathf.Atan2(dirToLookTarget.z, dirToLookTarget.x) * Mathf.Rad2Deg;

		while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetAngle)) > 0.05f)
		{
			float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime);
			transform.eulerAngles = Vector3.up * angle;
			state = State.turning;
			yield return null;
		}
	}

	/// <summary>
    /// Turns to chase player
    /// </summary>
	private void TurnToChase()
    {
		Vector3 dirToLookTarget = (Player.position - transform.position).normalized;
		float targetAngle = 90 - Mathf.Atan2(dirToLookTarget.z, dirToLookTarget.x) * Mathf.Rad2Deg;
		float angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, turnSpeed * Time.deltaTime);
		transform.eulerAngles = Vector3.up * angle;
		state = State.turning;
	}

	/// <summary>
    /// Attack method
    /// </summary>
	private void Attack()
    {
		if (!readyToAttack) return;

		//Set state
		state = State.attacking;

		//Invoke Attack Reset
		readyToAttack = false;
		Invoke(nameof(ResetAttack), attackCooldown);
	}

	/// <summary>
    /// Reset Attack Method
    /// </summary>
	private void ResetAttack(){
		readyToAttack = true;
    }

    /// <returns>
    /// Distance between two Vectors
    /// </returns>
	public static float GetDistance(Vector3 pos1,Vector3 pos2)
	{
		//Calculate distance using this formula
		Vector3 difV = pos1 - pos2;
		double a1 = Math.Pow(difV.x, 2f);
		double b1 = Math.Pow(difV.y, 2f);
		double c1 = Math.Pow(difV.z, 2f);

		//Finally, calculate distance
		float dist = (float)Math.Sqrt(a1 + b1 + c1);

		return (float)Math.Abs(dist);
	}

}

public enum State
{
	turning,
	walking,
	chasing,
	attacking,
	stopped
}