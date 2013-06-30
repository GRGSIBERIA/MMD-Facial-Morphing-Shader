using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MMDSkinsScript : MonoBehaviour
{
	// �\��̎��
	public enum SkinType
	{
		Base,
		EyeBrow,
		Eye,
		Lip,
		Other,
	}

	// �S�Ă̒��_�f�[�^����^�[�Q�b�g�ƂȂ钸�_�C���f�b�N�X
	public int[] targetIndices;

	// ���[�t��ւ̃x�N�g��
	public Vector3[] morphTarget;

	// �\��̎��
	public SkinType skinType;

	// �O�t���[���̃E�F�C�g�l
	float prev_weight = 0;
	
	// �E�F�C�g�t�����[�t����
	public Vector3[] current_morph=null;

	// Use this for initialization
	void Start () 
	{
		
	}

	// ���[�t�̌v�Z
	public bool Compute(Vector3[] composite)
	{
		bool computed_morph = false;	// �v�Z�������ǂ���

		float weight = transform.localPosition.z;
		
		if(current_morph==null || targetIndices.Length!=current_morph.Length)
			current_morph=new Vector3[targetIndices.Length];

		if (weight != prev_weight)
		{
			computed_morph = true;
			for (int i = 0; i < targetIndices.Length; i++)
				current_morph[i]=morphTarget[i] * weight;
		}
		for (int i = 0; i < targetIndices.Length; i++)
		{
			if(targetIndices[i]<composite.Length)
				composite[targetIndices[i]] += current_morph[i];
		}

		prev_weight = weight;
		return computed_morph;
	}

}
