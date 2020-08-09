using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface PlayerStateInterface
{
	void Init();

	bool HasBeenInitiated();

	void Update();

	void FixedUpdate();

	void Exit();

	bool HasExited();

	void ChangeState(PlayerStateInterface newState);

	void OnCollisionEnter(Collision hit);

	void OnCollisionStay(Collision hit);

	void OnCollisionExit(Collision hit);

	void OnDrawGizmos();
}
