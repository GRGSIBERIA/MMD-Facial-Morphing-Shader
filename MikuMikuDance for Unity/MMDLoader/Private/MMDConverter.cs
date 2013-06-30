﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;

namespace MMD
{
	namespace PMD
	{
		public class PMDConverter
		{
			Vector3[] EntryVertices(PMD.PMDFormat format)
			{
				int vcount = (int)format.vertex_list.vert_count;
				Vector3[] vpos = new Vector3[vcount];
				for (int i = 0; i < vcount; i++)
					vpos[i] = format.vertex_list.vertex[i].pos;
				return vpos;
			}
			
			Vector3[] EntryNormals(PMD.PMDFormat format)
			{
				int vcount = (int)format.vertex_list.vert_count;
				Vector3[] normals = new Vector3[vcount];
				for (int i = 0; i < vcount; i++)
					normals[i] = format.vertex_list.vertex[i].normal_vec;
				return normals;
			}
			
			Vector2[] EntryUVs(PMD.PMDFormat format)
			{
				int vcount = (int)format.vertex_list.vert_count;
				Vector2[] uvs = new Vector2[vcount];
				for (int i = 0; i < vcount; i++)
					uvs[i] = format.vertex_list.vertex[i].uv;
				return uvs;
			}
			
			BoneWeight[] EntryBoneWeights(PMD.PMDFormat format)
			{
				int vcount = (int)format.vertex_list.vert_count;
				BoneWeight[] weights = new BoneWeight[vcount];
				for (int i = 0; i < vcount; i++)
				{
					weights[i].boneIndex0 = (int)format.vertex_list.vertex[i].bone_num[0];
					weights[i].boneIndex1 = (int)format.vertex_list.vertex[i].bone_num[1];
					weights[i].weight0 = format.vertex_list.vertex[i].bone_weight;
					weights[i].weight1 = 100 - format.vertex_list.vertex[i].bone_weight;
				}
				return weights;
			}
			
			// 頂点座標やUVなどの登録だけ
			void EntryAttributesForMesh(PMD.PMDFormat format, Mesh mesh)
			{
				//mesh.vertexCount = (int)format.vertex_list.vert_count;
				mesh.vertices = EntryVertices(format);
				mesh.normals = EntryNormals(format);
				mesh.uv = EntryUVs(format);
				mesh.boneWeights = EntryBoneWeights(format);
			}
			
			void SetSubMesh(PMD.PMDFormat format, Mesh mesh)
			{
				// マテリアル対サブメッシュ
				// サブメッシュとはマテリアルに適用したい面頂点データのこと
				// 面ごとに設定するマテリアルはここ
				mesh.subMeshCount = (int)format.material_list.material_count;
				
				int sum = 0;
				for (int i = 0; i < mesh.subMeshCount; i++)
				{
					int count = (int)format.material_list.material[i].face_vert_count;
					int[] indices = new int[count];
					
					// 面頂点は材質0から順番に加算されている
					for (int j = 0; j < count; j++)
						indices[j] = format.face_vertex_list.face_vert_index[j+sum];
					mesh.SetTriangles(indices, i);
					sum += (int)format.material_list.material[i].face_vert_count;
				}
			}
			
			// メッシュをProjectに登録
			void CreateAssetForMesh(PMD.PMDFormat format, Mesh mesh)
			{
				AssetDatabase.CreateAsset(mesh, format.folder + "/" + format.name + ".asset");
			}
			
			public Mesh CreateMesh(PMD.PMDFormat format)
			{
				Mesh mesh = new Mesh();
				EntryAttributesForMesh(format, mesh);
				SetSubMesh(format, mesh);
				CreateAssetForMesh(format, mesh);
				return mesh;
			}
			
			// 色の生成
			void EntryColors(PMD.PMDFormat format, Material[] mats, ShaderType shader_type)
			{
				// マテリアルの生成 
				for (int i = 0; i < mats.Length; i++)
				{
					// PMDフォーマットのマテリアルを取得 
					PMD.PMDFormat.Material pmdMat = format.material_list.material[i];
					
					switch (shader_type)
					{
						case ShaderType.Default:	// デフォルト
							mats[i] = new Material(Shader.Find("Transparent/Diffuse"));
							mats[i].color = pmdMat.diffuse_color;
							Color cbuf = mats[i].color;
							cbuf.a = pmdMat.alpha;	// これでいいのか？
							mats[i].color = cbuf;
							break;

						case ShaderType.HalfLambert:	// ハーフランバート
							mats[i] = new Material(Shader.Find("Custom/CharModel"));
							mats[i].SetFloat("_Cutoff", 1 - pmdMat.alpha);
							mats[i].color = pmdMat.diffuse_color;
							break;

						case ShaderType.MMDShader:
							if (pmdMat.edge_flag == 1)
							{	// エッジがあるよ
								mats[i] = new Material(Shader.Find("MMD/Transparent/PMDMaterial-with-Outline"));
								mats[i].SetFloat("_OutlineWidth", 0.2f);	// これぐらいがいい気がする
							}
							else
							{
								mats[i] = new Material(Shader.Find("MMD/Transparent/PMDMaterial"));
							}
							mats[i].SetColor("_Color", pmdMat.diffuse_color);
							mats[i].SetColor("_AmbColor", pmdMat.mirror_color);
							mats[i].SetFloat("_Opacity", pmdMat.alpha);
							mats[i].SetColor("_SpecularColor", pmdMat.specular_color);
							mats[i].SetFloat("_Shininess", pmdMat.specularity);

							// ここでスフィアマップ
							string path = format.folder + "/" + pmdMat.sphere_map_name;
							Texture sphere_map;

							if (File.Exists(path))
							{	//　ファイルの存在を確認
								sphere_map = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Texture)) as Texture;
								
								// 乗算と加算判定
								string ext = Path.GetExtension(pmdMat.sphere_map_name);
								if (ext == ".spa")
								{	// 加算
									mats[i].SetTexture("_SphereAddTex", sphere_map);
									mats[i].SetTextureScale("_SphereAddTex", new Vector2(1, -1));
								}
								else if (ext == ".sph")
								{	// 乗算
									mats[i].SetTexture("_SphereMulTex", sphere_map);
									mats[i].SetTextureScale("_SphereMulTex", new Vector2(1, -1));
								}
							}

							// トゥーンの位置を取得
							string toon_name = pmdMat.toon_index != 0xFF ?
								format.toon_texture_list.toon_texture_file[pmdMat.toon_index] : "toon00.bmp";
							string resource_path = UnityEditor.AssetDatabase.GetAssetPath(Shader.Find("MMD/HalfLambertOutline"));
							resource_path = Path.GetDirectoryName(resource_path);	// resourceディレクトリを取得
							resource_path += "/toon/" + toon_name;

							// トゥーンが存在するか確認
							if (!File.Exists(resource_path))
							{
								// 自前トゥーンの可能性がある
								resource_path = format.folder + "/" + format.toon_texture_list.toon_texture_file[pmdMat.toon_index];
								if (!File.Exists(resource_path))
								{
									Debug.LogError("Do not exists toon texture: " + format.toon_texture_list.toon_texture_file[pmdMat.toon_index]);
									break;
								}
							}

							// テクスチャの割り当て
							Texture toon_tex = UnityEditor.AssetDatabase.LoadAssetAtPath(resource_path, typeof(Texture)) as Texture;
							mats[i].SetTexture("_ToonTex", toon_tex);
							mats[i].SetTextureScale("_ToonTex", new Vector2(1, -1));
							break;
					}

					// テクスチャが空でなければ登録
					if (pmdMat.texture_file_name != "") {
						string path = format.folder + "/" + pmdMat.texture_file_name;
						mats[i].mainTexture = AssetDatabase.LoadAssetAtPath(path, typeof(Texture)) as Texture;
						mats[i].mainTextureScale = new Vector2(1, -1);
					}
				}
			}
			
			// マテリアルに必要な色などを登録
			Material[] EntryAttributesForMaterials(PMD.PMDFormat format)
			{
				int count = (int)format.material_list.material_count;
				Material[] mats = new Material[count];
				EntryColors(format, mats, format.shader_type);
				return mats;
			}
			
			// マテリアルの登録
			void CreateAssetForMaterials(PMD.PMDFormat format, Material[] mats)
			{
				// 適当なフォルダに投げる
				string path = format.folder + "/Materials/";
				if (!System.IO.Directory.Exists(path)) { 
					AssetDatabase.CreateFolder(format.folder, "Materials");
				}
				
				for (int i = 0; i < mats.Length; i++)
				{
					string fname = path + format.name + "_material" + i + ".asset";
					AssetDatabase.CreateAsset(mats[i], fname);
				}
			}
			
			// マテリアルの生成
			public Material[] CreateMaterials(PMD.PMDFormat format)
			{
				Material[] materials;
				materials = EntryAttributesForMaterials(format);
				CreateAssetForMaterials(format, materials);
				return materials;
			}

			// 親子関係の構築
			void AttachParentsForBone(PMD.PMDFormat format, GameObject[] bones)
			{
				for (int i = 0; i < bones.Length; i++)
				{
					int index = format.bone_list.bone[i].parent_bone_index;
					if (index != 0xFFFF)
						bones[i].transform.parent = bones[index].transform;
					else
						bones[i].transform.parent = format.caller.transform;
				}
			}

			// ボーンの位置決めや親子関係の整備など
			GameObject[] EntryAttributeForBones(PMD.PMDFormat format)
			{
				int count = format.bone_list.bone_count;
				GameObject[] bones = new GameObject[count];
				
				for (int i = 0; i < count; i++) {
					bones[i] = new GameObject(format.bone_list.bone[i].bone_name);
					bones[i].transform.name = bones[i].name;
					bones[i].transform.position = format.bone_list.bone[i].bone_head_pos;
				}
				return bones;
			}
			
			// ボーンの生成
			public GameObject[] CreateBones(PMD.PMDFormat format)
			{
				GameObject[] bones;
				bones = EntryAttributeForBones(format);
				AttachParentsForBone(format, bones);
				CreateSkinBone(format, bones);
				return bones;
			}

			// 表情ボーンの生成を行う
			void CreateSkinBone(PMD.PMDFormat format, GameObject[] bones)
			{
				// 表情ルートを生成してルートの子供に付ける
				GameObject skin_root = new GameObject("Expression");
				if (skin_root.GetComponent<ExpressionManagerScript>() == null)
					skin_root.AddComponent<ExpressionManagerScript>();
				skin_root.transform.parent = format.caller.transform;
				
				for (int i = 0; i < format.skin_list.skin_count; i++)
				{
					// 表情を親ボーンに付ける
					GameObject skin = new GameObject(format.skin_list.skin_data[i].skin_name);
					skin.transform.parent = skin_root.transform;
					var script = skin.AddComponent<MMDSkinsScript>();

					// モーフの情報を入れる
					AssignMorphVectorsForSkin(format.skin_list.skin_data[i], format.vertex_list, script);
				}
			}

			// モーフ情報（頂点インデックス、モーフ先頂点など）を記録する
			void AssignMorphVectorsForSkin(PMD.PMDFormat.SkinData data, PMD.PMDFormat.VertexList vtxs, MMDSkinsScript script)
			{
				uint count = data.skin_vert_count;
				int[] indices = new int[count];
				Vector3[] morph_target = new Vector3[count];

				for (int i = 0; i < count; i++)
				{
					// ここで設定する
					indices[i] = (int)data.skin_vert_data[i].skin_vert_index;

					// モーフ先 - 元頂点
					//morph_target[i] = (data.skin_vert_data[i].skin_vert_pos - vtxs.vertex[indices[i]].pos).normalized;
					//morph_target[i] = data.skin_vert_data[i].skin_vert_pos - vtxs.vertex[indices[i]].pos;
					morph_target[i] = data.skin_vert_data[i].skin_vert_pos;
				}

				// スクリプトに記憶させる
				script.morphTarget = morph_target;
				script.targetIndices = indices;

				switch (data.skin_type)
				{
					case 0:
						script.skinType = MMDSkinsScript.SkinType.Base;
						script.gameObject.name = "base";
						break;

					case 1:
						script.skinType = MMDSkinsScript.SkinType.EyeBrow;
						break;

					case 2:
						script.skinType = MMDSkinsScript.SkinType.Eye;
						break;

					case 3:
						script.skinType = MMDSkinsScript.SkinType.Lip;
						break;

					case 4:
						script.skinType = MMDSkinsScript.SkinType.Other;
						break;
				}
			}

			// バインドポーズの作成
			public void BuildingBindpose(PMD.PMDFormat format, Mesh mesh, Material[] materials, GameObject[] bones)
			{
				// 行列とかトランスフォームとか
				Matrix4x4[] bindpose = new Matrix4x4[bones.Length];
				Transform[] trans = new Transform[bones.Length];
				for (int i = 0; i < bones.Length; i++) {
					trans[i] = bones[i].transform;
					bindpose[i] = bones[i].transform.worldToLocalMatrix;
				}
				
				// ここで本格的な適用
				SkinnedMeshRenderer smr = format.caller.AddComponent<SkinnedMeshRenderer>() as SkinnedMeshRenderer;
				mesh.bindposes = bindpose;
				smr.sharedMesh = mesh;
				smr.bones = trans;
				smr.materials = materials;
			}
			
			// IKの登録
			//   IKは基本的にスクリプトを利用
			public CCDIKSolver[] EntryIKSolver(PMD.PMDFormat format, GameObject[] bones)
			{
				PMD.PMDFormat.IKList ik_list = format.ik_list;

				CCDIKSolver[] iksolvers = new CCDIKSolver[ik_list.ik_data_count];
				for (int i = 0; i < ik_list.ik_data_count; i++)
				{
					PMD.PMDFormat.IK ik = ik_list.ik_data[i];

					bones[ik.ik_bone_index].AddComponent<CCDIKSolver>();
					CCDIKSolver solver = bones[ik.ik_bone_index].GetComponent<CCDIKSolver>();
					solver.target = bones[ik.ik_target_bone_index].transform;
					solver.controll_weight = ik.control_weight * 4; // PMDファイルは4倍らしい
					solver.iterations = ik.iterations;
					solver.chains = new Transform[ik.ik_chain_length];
					for (int j = 0; j < ik.ik_chain_length; j++)
						solver.chains[j] = bones[ik.ik_child_bone_index[j]].transform;

					if (!(bones[ik.ik_bone_index].name.Contains("足") || bones[ik.ik_bone_index].name.Contains("つま先")))
					{
						solver.enabled = false;
					}
					iksolvers[i] = solver;
				}

				return iksolvers;
			}

			// Sphere Colliderの設定
			Collider EntrySphereCollider(PMDFormat.Rigidbody rigid, GameObject obj)
			{
				SphereCollider collider = obj.AddComponent<SphereCollider>();
				collider.radius = rigid.shape_w;
				return collider;
			}

			// Box Colliderの設定
			Collider EntryBoxCollider(PMDFormat.Rigidbody rigid, GameObject obj)
			{
				BoxCollider collider = obj.AddComponent<BoxCollider>();
				collider.size = new Vector3(
					rigid.shape_w * 2f,
					rigid.shape_h * 2f, 
					rigid.shape_d * 2f);
				return collider;
			}

			// Capsule Colliderの設定
			Collider EntryCapsuleCollider(PMDFormat.Rigidbody rigid, GameObject obj)
			{
				CapsuleCollider collider = obj.AddComponent<CapsuleCollider>();
				collider.radius = rigid.shape_w;
				collider.height = rigid.shape_h + rigid.shape_w * 2;
				return collider;
			}

			// 物理素材の定義
			PhysicMaterial CreatePhysicMaterial(PMDFormat format, PMDFormat.Rigidbody rigid)
			{
				PhysicMaterial material = new PhysicMaterial(format.name + "_r" + rigid.rigidbody_name);
				material.bounciness = rigid.rigidbody_recoil;
				material.staticFriction = rigid.rigidbody_friction;
				material.dynamicFriction = rigid.rigidbody_friction;

				AssetDatabase.CreateAsset(material, format.folder + "/Physics/" + material.name + ".asset");
				return material;
			}

			// Unity側のRigidbodyの設定を行う
			void UnityRigidbodySetting(PMDFormat.Rigidbody rigidbody, GameObject targetBone, bool setted=false)
			{
				// rigidbodyの調整
				if (!setted)
				{
					targetBone.rigidbody.isKinematic = rigidbody.rigidbody_type != 0 ? false : true;
					targetBone.rigidbody.mass = rigidbody.rigidbody_weight;
					targetBone.rigidbody.drag = rigidbody.rigidbody_pos_dim;
					targetBone.rigidbody.angularDrag = rigidbody.rigidbody_rot_dim;
				}
				else
				{
					// Rigidbodyはボーンに対して適用されるので複数ある場合は平均を取る
					targetBone.rigidbody.mass += rigidbody.rigidbody_weight;
					targetBone.rigidbody.drag += rigidbody.rigidbody_pos_dim;
					targetBone.rigidbody.angularDrag += rigidbody.rigidbody_rot_dim;
					targetBone.rigidbody.mass *= 0.5f;
					targetBone.rigidbody.drag *= 0.5f;
					targetBone.rigidbody.angularDrag *= 0.5f;
				}
			}

			// 剛体の値を代入する
			public void SetRigidsSettings(PMDFormat format, GameObject[] bones, GameObject[] rigid)
			{
				PMDFormat.RigidbodyList list = format.rigidbody_list;
				for (int i = 0; i < list.rigidbody_count; i++)	// iは剛体番号
				{
					// 剛体の関連ボーンのインデックス
					int rigidRefIndex = list.rigidbody[i].rigidbody_rel_bone_index;

					// ローカル座標の確定
					Vector3 localPos = list.rigidbody[i].pos_pos;// - rigid[i].transform.position;

					// ここで位置の決定
					if (rigidRefIndex >= ushort.MaxValue)
					{	// indexが見つからない場合、関連ボーンが存在しないのでなんとかする
						
						// 関連ボーンなしの剛体はセンターボーンに接続している
						rigid[i].transform.position = localPos + format.bone_list.bone[0].bone_head_pos;
					}
					else
					{	// とりあえずここで剛体を追加・設定
						if (bones[rigidRefIndex].rigidbody == null)
							bones[rigidRefIndex].AddComponent<Rigidbody>();
						UnityRigidbodySetting(list.rigidbody[i], bones[rigidRefIndex]);
						rigid[i].transform.localPosition = localPos;
					}
					
					// 回転の値を決める
					Vector3 rot = list.rigidbody[i].pos_rot * Mathf.Rad2Deg;
					rigid[i].transform.rotation = Quaternion.Euler(rot);
				}
			}

			// 剛体の生成
			public GameObject[] CreateRigids(PMDFormat format, GameObject[] bones)
			{
				PMDFormat.RigidbodyList list = format.rigidbody_list;
				if (!System.IO.Directory.Exists(System.IO.Path.Combine(format.folder, "Physics")))
				{
					AssetDatabase.CreateFolder(format.folder, "Physics");
				}
				
				// 剛体の登録 
				GameObject[] rigid = new GameObject[list.rigidbody_count];
				for (int i = 0; i < list.rigidbody_count; i++)
				{
					rigid[i] = new GameObject("r" + list.rigidbody[i].rigidbody_name);
					//rigid[i].AddComponent<Rigidbody>();		// 剛体本体にはrigidbodyは適用しない

					// 各種Colliderの設定
					Collider collider = null;
					switch (list.rigidbody[i].shape_type)
					{
						case 0:
							collider = EntrySphereCollider(list.rigidbody[i], rigid[i]);
							break;

						case 1:
							collider = EntryBoxCollider(list.rigidbody[i], rigid[i]);
							break;

						case 2:
							collider = EntryCapsuleCollider(list.rigidbody[i], rigid[i]);
							break;
					}

					// マテリアルの設定
					collider.material = CreatePhysicMaterial(format, list.rigidbody[i]);
				}
				return rigid;
			}

			// 接続剛体Bの番号と一致するJointを探し出す
			int SearchConnectJointByConnectB(int connectBIndex, PMDFormat format)
			{
				for (int i = 0; i < format.rigidbody_joint_list.joint_count; i++)
				{
					if (format.rigidbody_joint_list.joint[i].joint_rigidbody_b == connectBIndex)
						return i;
				}
				return -1;
			}

			// 接続剛体Aの剛体番号を検索する
			int SearchConnectRigidA(int rigidIndex, PMDFormat format)
			{
				// ジョイントから接続剛体B＝現在の剛体名で探し出す
				int jointB;
				for (jointB = 0; jointB < format.rigidbody_joint_list.joint_count; jointB++)
				{
					if (format.rigidbody_joint_list.joint[jointB].joint_rigidbody_b == rigidIndex)
					{
						break;
					}
				}
				// targetRigidは接続剛体A
				return (int)format.rigidbody_joint_list.joint[jointB].joint_rigidbody_a;
			}

			// 関連ボーンなしの剛体から親のボーンを探してくる
			// rigidIndexは剛体番号
			int GetTargetRigidBone(int rigidIndex, PMDFormat format)
			{
				// 接続剛体Aを探す
				int targetRigid = SearchConnectRigidA(rigidIndex, format);

				// 接続剛体Aの関連ボーンを探す
				int ind = format.rigidbody_list.rigidbody[targetRigid].rigidbody_rel_bone_index;
				
				// MaxValueを引けば接続剛体Aの関連ボーンに接続されるようになっている
				if (ind >= ushort.MaxValue)
					format.rigidbody_list.rigidbody[rigidIndex].rigidbody_rel_bone_index = ushort.MaxValue + (ushort)ind;
				
				return (int)ind;
			}

			// 剛体ボーンを
			public void AssignRigidbodyToBone(PMDFormat format, GameObject[] bones, GameObject[] rigids)
			{
				// 剛体の数だけ回す
				for (int i = 0; i < rigids.Length; i++)
				{
					// 剛体を親ボーンに格納
					int refIndex = format.rigidbody_list.rigidbody[i].rigidbody_rel_bone_index;
					if (refIndex != ushort.MaxValue)
					{	// 65535が最大値
						rigids[i].transform.parent = bones[refIndex].transform;
					}
					else
					{
						// ジョイントから接続剛体B＝現在の剛体名で探し出す
						int boneIndex = GetTargetRigidBone(i, format);

						// 接続剛体Aの関連ボーンに剛体を接続
						rigids[i].transform.parent = bones[boneIndex].transform;
					}
				}
			}

			void SearchEqualJoint(PMDFormat format, int jointIndex, GameObject[] bones)
			{
				// 対象のJointに親のジョイントがいなければ末端のジョイント
				// 対象のJointの接続剛体Aが差している関連ボーンにFixedJointを設定する
				if (jointIndex == ushort.MaxValue) return;	// 存在しない値の場合は飛ばす
				PMDFormat.Joint targetJoint = format.rigidbody_joint_list.joint[jointIndex];
				for (int connectRigidAJoint = 0; connectRigidAJoint < format.rigidbody_joint_list.joint_count; connectRigidAJoint++)
				{
					if (connectRigidAJoint == ushort.MaxValue) continue;
					PMDFormat.Joint parentJoint = format.rigidbody_joint_list.joint[connectRigidAJoint];
					if (targetJoint.joint_rigidbody_a == parentJoint.joint_rigidbody_b)
					{
						// targetJointが末端のジョイントだったのでFixedJointを追加する
						PMDFormat.Rigidbody tailRigid = format.rigidbody_list.rigidbody[targetJoint.joint_rigidbody_a];
						int parentIndex = format.bone_list.bone[tailRigid.rigidbody_rel_bone_index].parent_bone_index;
						
						// 既にジョイントが追加されている場合は抜ける
						ConfigurableJoint c = bones[parentIndex].GetComponent<ConfigurableJoint>();
						if (c != null) break;
						FixedJoint j = bones[parentIndex].GetComponent<FixedJoint>();
						if (j != null) break;

						bones[parentIndex].AddComponent<FixedJoint>();	// 対象の親ボーンにFixed
						bones[parentIndex].GetComponent<Rigidbody>().isKinematic = true;
						return;
					}
				}
			}

			// FixedJointの設定
			void SetupFixedJoint(PMDFormat format, GameObject[] bones)
			{
				// 全てのJointを探索する
				for (int jointIndex = 0; jointIndex < format.rigidbody_joint_list.joint_count; jointIndex++)
				{
					SearchEqualJoint(format, jointIndex, bones);
				}
			}

			// 移動や回転制限
			void SetMotionAngularLock(PMDFormat.Joint joint, ConfigurableJoint conf)
			{
				SoftJointLimit jlim;

				// Motionの固定
				if (joint.constrain_pos_1.x == 0f && joint.constrain_pos_2.x == 0f)
					conf.xMotion = ConfigurableJointMotion.Locked;
				else
					conf.xMotion = ConfigurableJointMotion.Limited;

				if (joint.constrain_pos_1.y == 0f && joint.constrain_pos_2.y == 0f)
					conf.yMotion = ConfigurableJointMotion.Locked;
				else
					conf.yMotion = ConfigurableJointMotion.Limited;

				if (joint.constrain_pos_1.z == 0f && joint.constrain_pos_2.z == 0f)
					conf.zMotion = ConfigurableJointMotion.Locked;
				else
					conf.zMotion = ConfigurableJointMotion.Limited;

				// 角度の固定
				if (joint.constrain_rot_1.x == 0f && joint.constrain_rot_2.x == 0f)
					conf.angularXMotion = ConfigurableJointMotion.Locked;
				else
				{
					conf.angularXMotion = ConfigurableJointMotion.Limited;
					float hlim = Mathf.Max(joint.constrain_rot_1.x , joint.constrain_rot_2.x);
					float llim = Mathf.Min(joint.constrain_rot_1.x , joint.constrain_rot_2.x);
					SoftJointLimit jhlim = new SoftJointLimit();
					jhlim.limit = hlim * Mathf.Rad2Deg;
					conf.highAngularXLimit = jhlim;

					SoftJointLimit jllim = new SoftJointLimit();
					jllim.limit = llim * Mathf.Rad2Deg;
					conf.lowAngularXLimit = jllim;
				}

				if (joint.constrain_rot_1.y == 0f && joint.constrain_rot_2.y == 0f)
					conf.angularYMotion = ConfigurableJointMotion.Locked;
				else
				{
					// 値がマイナスだとエラーが出るので注意
					conf.angularYMotion = ConfigurableJointMotion.Limited;
					float lim = Mathf.Max(Mathf.Abs(joint.constrain_rot_1.y),Mathf.Abs(joint.constrain_rot_2.y));//絶対値の大きいほう
					jlim = new SoftJointLimit();
					jlim.limit = lim * Mathf.Rad2Deg;
					conf.angularYLimit = jlim;
				}

				if (joint.constrain_rot_1.z == 0f && joint.constrain_rot_2.z == 0f)
					conf.angularZMotion = ConfigurableJointMotion.Locked;
				else
				{
					conf.angularZMotion = ConfigurableJointMotion.Limited;
					float lim = Mathf.Max(Mathf.Abs(joint.constrain_rot_1.z),Mathf.Abs(joint.constrain_rot_2.z));//絶対値の大きいほう
					jlim = new SoftJointLimit();
					jlim.limit = lim * Mathf.Rad2Deg;
					conf.angularZLimit = jlim;
				}
			}

			// ばねの設定など
			void SetDrive(PMDFormat.Joint joint, ConfigurableJoint conf)
			{
				JointDrive drive;

				// Position
				if (joint.spring_pos.x != 0f)
				{
					drive = new JointDrive();
					drive.positionSpring = joint.spring_pos.x;
					conf.xDrive = drive;
				}
				if (joint.spring_pos.y != 0f)
				{
					drive = new JointDrive();
					drive.positionSpring = joint.spring_pos.y;
					conf.yDrive = drive;
				}
				if (joint.spring_pos.z != 0f)
				{
					drive = new JointDrive();
					drive.positionSpring = joint.spring_pos.z;
					conf.zDrive = drive;
				}

				// Angular
				if (joint.spring_rot.x != 0f)
				{
					drive = new JointDrive();
					drive.mode = JointDriveMode.PositionAndVelocity;
					drive.positionSpring = joint.spring_rot.x;
					conf.angularXDrive = drive;
				}
				if (joint.spring_rot.y != 0f || joint.spring_rot.z != 0f)
				{
					drive = new JointDrive();
					drive.mode = JointDriveMode.PositionAndVelocity;
					drive.positionSpring = (joint.spring_rot.y + joint.spring_rot.z) * 0.5f;
					conf.angularYZDrive = drive;
				}
			}

			// ConfigurableJointの値を設定する, addedは既に設定がある
			void SetAttributeConfigurableJoint(PMDFormat.Joint joint, ConfigurableJoint conf, bool added)
			{
				if (!added)
				{
					SetMotionAngularLock(joint, conf);
					SetDrive(joint, conf);
				}
				else
				{
					//Debug.Log("added");
				}
			}

			GameObject GetParentBone(GameObject[] bones, GameObject target)
			{
				foreach (var b in bones)
				{
					if (target.transform.parent == b.transform)
						return b;
				}
				return null;
			}

			// ConfigurableJointの設定
			// 先に設定してからFixedJointを設定する
			void SetupConfigurableJoint(PMDFormat format, GameObject[] bones)
			{
				for (int jointIndex = 0; jointIndex < format.rigidbody_joint_list.joint_count; jointIndex++)
				{
					PMDFormat.Joint joint = format.rigidbody_joint_list.joint[jointIndex];
					ConfigurableJoint configurate = null;

					int jointedBone = format.rigidbody_list.rigidbody[joint.joint_rigidbody_b].rigidbody_rel_bone_index;
					if (jointedBone >= ushort.MaxValue)
					{
						// ボーンがないので飛ばす
						continue;
					}
					else
					{
						configurate = bones[jointedBone].GetComponent<ConfigurableJoint>();
						if (configurate != null)
						{	// すでに追加されていた場合
							SetAttributeConfigurableJoint(joint, configurate, true);
							continue;
						}

						configurate = bones[jointedBone].AddComponent<ConfigurableJoint>();
						SetAttributeConfigurableJoint(joint, configurate, false);

						// ここでジョイントに接続するrigidbodyを設定
						configurate.connectedBody = GetParentBone(bones, bones[jointedBone]).rigidbody;

						// nullってたら自動的に親ボーンの剛体に設定する
						//NullTestJointForRigid(configurate, bones[jointedBone]);
					}
					
				}
			}

			void NullTestConfigurableJoint(GameObject[] bones)
			{
				// JointのconnectedJointが空だった場合の回避策
				foreach (var b in bones)
				{
					var conf = b.GetComponent<ConfigurableJoint>();
					if (conf)
					{
						if (!conf.connectedBody)
						{	// 空なJointを探してきて親ボーンの剛体につなげる
							conf.connectedBody = b.transform.parent.gameObject.rigidbody;
							b.rigidbody.isKinematic = true;
							continue;
						}
					}
					else
					{	// fixedJointとだいたい同じ
						var fix = b.GetComponent<FixedJoint>();
						if (fix)
						{
							Debug.Log("fix:" + b.name);
							if (!fix.connectedBody)
							{
								fix.connectedBody = b.transform.parent.gameObject.rigidbody;
								continue;
							}
						}
					}
				}
			}

			// ジョイントの設定
			// ジョイントはボーンに対して適用される
			public void SettingJointComponent(PMDFormat format, GameObject[] bones, GameObject[] rigids)
			{
				// ConfigurableJointの設定
				SetupConfigurableJoint(format, bones);

				// FixedJointの設定
				SetupFixedJoint(format, bones);

				// ボーンを総なめして空のConfigurableJointが存在するか調べる
				NullTestConfigurableJoint(bones);
			}

			// 非衝突剛体の設定
			public List<int>[] SettingIgnoreRigidGroups(PMDFormat format, GameObject[] rigids)
			{
				// 非衝突グループ用リストの初期化
				const int MaxGroup = 16;	// グループの最大数
				List<int>[] ignoreRigid = new List<int>[MaxGroup];
				for (int i = 0; i < 16; i++) ignoreRigid[i] = new List<int>();

				// それぞれの剛体が所属している非衝突グループを追加していく
				PMDFormat.RigidbodyList list = format.rigidbody_list;
				for (int i = 0; i < list.rigidbody_count; i++)
					ignoreRigid[list.rigidbody[i].rigidbody_group_index].Add(i);
				return ignoreRigid;
			}

			// グループターゲット
			public int[] GetRigidbodyGroupTargets(PMDFormat format, GameObject[] rigids)
			{
				int[] result = new int[rigids.Length];
				for (int i = 0; i < rigids.Length; i++)
				{
					result[i] = format.rigidbody_list.rigidbody[i].rigidbody_group_target;
				}
				return result;
			}
		}
	}
	
	namespace VMD
	{
		public class VMDConverter
		{
			// ベジェハンドルを取得する
			// 0～127の値を 0f～1fとして返す
			static Vector2 GetBezierHandle(byte[] interpolation, int type, int ab)
			{
				// 0=X, 1=Y, 2=Z, 3=R
				// abはa?かb?のどちらを使いたいか
				Vector2 bezierHandle = new Vector2((float)interpolation[ab*8+type], (float)interpolation[ab*8+4+type]);
				return bezierHandle/127f;
			}
			// p0:(0f,0f),p3:(1f,1f)のベジェ曲線上の点を取得する
			// tは0～1の範囲
			static Vector2 SampleBezier(Vector2 bezierHandleA, Vector2 bezierHandleB, float t)
			{
				Vector2 p0 = Vector2.zero;
				Vector2 p1 = bezierHandleA;
				Vector2 p2 = bezierHandleB;
				Vector2 p3 = new Vector2(1f,1f);
				
				Vector2 q0 = Vector2.Lerp(p0, p1, t);
				Vector2 q1 = Vector2.Lerp(p1, p2, t);
				Vector2 q2 = Vector2.Lerp(p2, p3, t);
				
				Vector2 r0 = Vector2.Lerp(q0, q1, t);
				Vector2 r1 = Vector2.Lerp(q1, q2, t);
				
				Vector2 s0 = Vector2.Lerp(r0, r1, t);
				return s0;
			}
			// 補間曲線が線形補間と等価か
			static bool IsLinear(byte[] interpolation, int type)
			{
				byte ax=interpolation[0*8+type];
				byte ay=interpolation[0*8+4+type];
				byte bx=interpolation[1*8+type];
				byte by=interpolation[1*8+4+type];
				return (ax == ay) && (bx == by);
			}
			// 補間曲線の近似のために追加するキーフレームを含めたキーフレーム数を取得する
			int GetKeyframeCount(List<MMD.VMD.VMDFormat.Motion> mlist, int type, int interpolationQuality)
			{
				int count = 0;
				for(int i = 0; i < mlist.Count; i++)
				{
					if(i>0 && !IsLinear(mlist[i].interpolation, type))
					{
						count += interpolationQuality;//Interpolation Keyframes
					}
					else
					{
						count += 1;//Keyframe
					}
				}
				return count;
			}
			//キーフレームが1つの時、ダミーキーフレームを追加する
			void AddDummyKeyframe(ref Keyframe[] keyframes)
			{
				if(keyframes.Length==1)
				{
					Keyframe[] newKeyframes=new Keyframe[2];
					newKeyframes[0]=keyframes[0];
					newKeyframes[1]=keyframes[0];
					newKeyframes[1].time+=0.001f/60f;//1[ms]
					newKeyframes[0].outTangent=0f;
					newKeyframes[1].inTangent=0f;
					keyframes=newKeyframes;
				}
			}
			// 任意の型のvalueを持つキーフレーム
			abstract class CustomKeyframe<Type>
			{
				public CustomKeyframe(float time,Type value)
				{
					this.time=time;
					this.value=value;
				}
				public float time{ get; set; }
				public Type value{ get; set; }
			}
			// float型のvalueを持つキーフレーム
			class FloatKeyframe:CustomKeyframe<float>
			{
				public FloatKeyframe(float time,float value):base(time,value)
				{
				}
				// 線形補間
				public static FloatKeyframe Lerp(FloatKeyframe from, FloatKeyframe to,Vector2 t)
				{
					return new FloatKeyframe(
						Mathf.Lerp(from.time,to.time,t.x),
						Mathf.Lerp(from.value,to.value,t.y)
					);
				}
				// ベジェを線形補間で近似したキーフレームを追加する
				public static void AddBezierKeyframes(byte[] interpolation, int type,
					FloatKeyframe prev_keyframe,FloatKeyframe cur_keyframe, int interpolationQuality,
					ref FloatKeyframe[] keyframes,ref int index)
				{
					if(prev_keyframe==null || IsLinear(interpolation,type))
					{
						keyframes[index++]=cur_keyframe;
					}
					else
					{
						Vector2 bezierHandleA=GetBezierHandle(interpolation,type,0);
						Vector2 bezierHandleB=GetBezierHandle(interpolation,type,1);
						int sampleCount = interpolationQuality;
						for(int j = 0; j < sampleCount; j++)
						{
							float t = (j+1)/(float)sampleCount;
							Vector2 sample = SampleBezier(bezierHandleA,bezierHandleB,t);
							keyframes[index++] = FloatKeyframe.Lerp(prev_keyframe,cur_keyframe,sample);
						}
					}
				}
			}
			// Quaternion型のvalueを持つキーフレーム
			class QuaternionKeyframe:CustomKeyframe<Quaternion>
			{
				public QuaternionKeyframe(float time,Quaternion value):base(time,value)
				{
				}
				// 線形補間
				public static QuaternionKeyframe Lerp(QuaternionKeyframe from, QuaternionKeyframe to,Vector2 t)
				{
					return new QuaternionKeyframe(
						Mathf.Lerp(from.time,to.time,t.x),
						Quaternion.Slerp(from.value,to.value,t.y)
					);
				}
				// ベジェを線形補間で近似したキーフレームを追加する
				public static void AddBezierKeyframes(byte[] interpolation, int type,
					QuaternionKeyframe prev_keyframe,QuaternionKeyframe cur_keyframe, int interpolationQuality,
					ref QuaternionKeyframe[] keyframes,ref int index)
				{
					if(prev_keyframe==null || IsLinear(interpolation,type))
					{
						keyframes[index++]=cur_keyframe;
					}
					else
					{
						Vector2 bezierHandleA=GetBezierHandle(interpolation,type,0);
						Vector2 bezierHandleB=GetBezierHandle(interpolation,type,1);
						int sampleCount = interpolationQuality;
						for(int j = 0; j < sampleCount; j++)
						{
							float t=(j+1)/(float)sampleCount;
							Vector2 sample = SampleBezier(bezierHandleA,bezierHandleB,t);
							keyframes[index++] = QuaternionKeyframe.Lerp(prev_keyframe,cur_keyframe,sample);
						}
					}
				}
				
			}
			
			//移動の線形補間用tangentを求める 
			float GetLinearTangentForPosition(Keyframe from_keyframe,Keyframe to_keyframe)
			{
				return (to_keyframe.value-from_keyframe.value)/(to_keyframe.time-from_keyframe.time);
			}
			//-359～+359度の範囲を等価な0～359度へ変換する。
			float Mod360(float angle)
			{
				//剰余演算の代わりに加算にする
				return (angle<0)?(angle+360f):(angle);
			}
			//回転の線形補間用tangentを求める
			float GetLinearTangentForRotation(Keyframe from_keyframe,Keyframe to_keyframe)
			{
				float tv=Mod360(to_keyframe.value);
				float fv=Mod360(from_keyframe.value);
				float delta_value=Mod360(tv-fv);
				//180度を越える場合は逆回転
				if(delta_value<180f)
				{ 
					return delta_value/(to_keyframe.time-from_keyframe.time);
				}
				else
				{
					return (delta_value-360f)/(to_keyframe.time-from_keyframe.time);
				}
			}
			//アニメーションエディタでBothLinearを選択したときの値
			private const int TangentModeBothLinear=21;
			
			//UnityのKeyframeに変換する（回転用）
			void ToKeyframesForRotation(QuaternionKeyframe[] custom_keys,ref Keyframe[] rx_keys,ref Keyframe[] ry_keys,ref Keyframe[] rz_keys)
			{
				rx_keys=new Keyframe[custom_keys.Length];
				ry_keys=new Keyframe[custom_keys.Length];
				rz_keys=new Keyframe[custom_keys.Length];
				for(int i = 0; i < custom_keys.Length; i++)
				{
					//オイラー角を取り出す
					Vector3 eulerAngles=custom_keys[i].value.eulerAngles;
					rx_keys[i]=new Keyframe(custom_keys[i].time,eulerAngles.x);
					ry_keys[i]=new Keyframe(custom_keys[i].time,eulerAngles.y);
					rz_keys[i]=new Keyframe(custom_keys[i].time,eulerAngles.z);
					//線形補間する
					rx_keys[i].tangentMode=TangentModeBothLinear;
					ry_keys[i].tangentMode=TangentModeBothLinear;
					rz_keys[i].tangentMode=TangentModeBothLinear;
					if(i>0)
					{
						float tx=GetLinearTangentForRotation(rx_keys[i-1],rx_keys[i]);
						float ty=GetLinearTangentForRotation(ry_keys[i-1],ry_keys[i]);
						float tz=GetLinearTangentForRotation(rz_keys[i-1],rz_keys[i]);
						rx_keys[i-1].outTangent=tx;
						ry_keys[i-1].outTangent=ty;
						rz_keys[i-1].outTangent=tz;
						rx_keys[i].inTangent=tx;
						ry_keys[i].inTangent=ty;
						rz_keys[i].inTangent=tz;
					}
				}
				AddDummyKeyframe(ref rx_keys);
				AddDummyKeyframe(ref ry_keys);
				AddDummyKeyframe(ref rz_keys);
			}
			
			
			// あるボーンに含まれるキーフレを抽出
			// これは回転のみ
			void CreateKeysForRotation(MMD.VMD.VMDFormat format, AnimationClip clip, string current_bone, string bone_path, int interpolationQuality)
			{
				try 
				{
					List<MMD.VMD.VMDFormat.Motion> mlist = format.motion_list.motion[current_bone];
					int keyframeCount = GetKeyframeCount(mlist, 3, interpolationQuality);
					
					QuaternionKeyframe[] r_keys = new QuaternionKeyframe[keyframeCount];
					QuaternionKeyframe r_prev_key=null;
					int ir=0;
					for (int i = 0; i < mlist.Count; i++)
					{
						const float tick_time = 1.0f / 30.0f;
						float tick = mlist[i].flame_no * tick_time;
						
						Quaternion rotation=mlist[i].rotation;
						QuaternionKeyframe r_cur_key=new QuaternionKeyframe(tick,rotation);
						QuaternionKeyframe.AddBezierKeyframes(mlist[i].interpolation,3,r_prev_key,r_cur_key,interpolationQuality,ref r_keys,ref ir);
						r_prev_key=r_cur_key;
					}
					
					Keyframe[] rx_keys=null;
					Keyframe[] ry_keys=null;
					Keyframe[] rz_keys=null;
					ToKeyframesForRotation(r_keys, ref rx_keys, ref ry_keys, ref rz_keys);
					AnimationCurve curve_x = new AnimationCurve(rx_keys);
					AnimationCurve curve_y = new AnimationCurve(ry_keys);
					AnimationCurve curve_z = new AnimationCurve(rz_keys);
					// ここで回転オイラー角をセット（補間はクォータニオン）
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.x",curve_x);
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.y",curve_y);
					AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"localEulerAngles.z",curve_z);
				}
				catch (KeyNotFoundException)
				{
					//Debug.LogError("互換性のないボーンが読み込まれました:" + bone_path);
				}
			}
			//UnityのKeyframeに変換する（移動用）
			Keyframe[] ToKeyframesForLocation(FloatKeyframe[] custom_keys)
			{
				Keyframe[] keys=new Keyframe[custom_keys.Length];
				for(int i = 0; i < custom_keys.Length; i++)
				{
					keys[i]=new Keyframe(custom_keys[i].time,custom_keys[i].value);
					//線形補間する
					keys[i].tangentMode=TangentModeBothLinear;
					if(i>0)
					{
						float t=GetLinearTangentForPosition(keys[i-1],keys[i]);
						keys[i-1].outTangent=t;
						keys[i].inTangent=t;
					}
				}
				AddDummyKeyframe(ref keys);
				return keys;
			}
			// 移動のみの抽出
			void CreateKeysForLocation(MMD.VMD.VMDFormat format, AnimationClip clip, string current_bone, string bone_path, int interpolationQuality, GameObject current_obj = null)
			{
				try
				{
					Vector3 default_position = Vector3.zero;
					if(current_obj != null)
						default_position = current_obj.transform.localPosition;
					
					List<MMD.VMD.VMDFormat.Motion> mlist = format.motion_list.motion[current_bone];
					
					int keyframeCountX = GetKeyframeCount(mlist, 0, interpolationQuality);
					int keyframeCountY = GetKeyframeCount(mlist, 1, interpolationQuality); 
					int keyframeCountZ = GetKeyframeCount(mlist, 2, interpolationQuality);
					
					FloatKeyframe[] lx_keys = new FloatKeyframe[keyframeCountX];
					FloatKeyframe[] ly_keys = new FloatKeyframe[keyframeCountY];
					FloatKeyframe[] lz_keys = new FloatKeyframe[keyframeCountZ];
					
					FloatKeyframe lx_prev_key=null;
					FloatKeyframe ly_prev_key=null;
					FloatKeyframe lz_prev_key=null;
					int ix=0;
					int iy=0;
					int iz=0;
					for (int i = 0; i < mlist.Count; i++)
					{
						const float tick_time = 1.0f / 30.0f;
						
						float tick = mlist[i].flame_no * tick_time;
						
						FloatKeyframe lx_cur_key=new FloatKeyframe(tick,mlist[i].location.x + default_position.x);
						FloatKeyframe ly_cur_key=new FloatKeyframe(tick,mlist[i].location.y + default_position.y);
						FloatKeyframe lz_cur_key=new FloatKeyframe(tick,mlist[i].location.z + default_position.z);
						
						// 各軸別々に補間が付いてる
						FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,0,lx_prev_key,lx_cur_key,interpolationQuality,ref lx_keys,ref ix);
						FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,1,ly_prev_key,ly_cur_key,interpolationQuality,ref ly_keys,ref iy);
						FloatKeyframe.AddBezierKeyframes(mlist[i].interpolation,2,lz_prev_key,lz_cur_key,interpolationQuality,ref lz_keys,ref iz);
						
						lx_prev_key=lx_cur_key;
						ly_prev_key=ly_cur_key;
						lz_prev_key=lz_cur_key;
					}
					
					// 回転ボーンの場合はデータが入ってないはず
					if (mlist.Count != 0)
					{
						AnimationCurve curve_x = new AnimationCurve(ToKeyframesForLocation(lx_keys));
						AnimationCurve curve_y = new AnimationCurve(ToKeyframesForLocation(ly_keys));
						AnimationCurve curve_z = new AnimationCurve(ToKeyframesForLocation(lz_keys));
 						AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.x",curve_x);
						AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.y",curve_y);
						AnimationUtility.SetEditorCurve(clip,bone_path,typeof(Transform),"m_LocalPosition.z",curve_z);
					}
				}
				catch (KeyNotFoundException)
				{
					//Debug.LogError("互換性のないボーンが読み込まれました:" + current_bone);
				}
			}

			void CreateKeysForSkin(MMD.VMD.VMDFormat format, AnimationClip clip)
			{
				const float tick_time = 1f / 30f;

					// 全ての表情に打たれているキーフレームを探索
					List<VMD.VMDFormat.SkinData> s;

				foreach (var skin in format.skin_list.skin)
				{
					s = skin.Value;
					Keyframe[] keyframe = new Keyframe[skin.Value.Count];

					// キーフレームの登録を行う
					for (int i = 0; i < skin.Value.Count; i++) 
					{
						keyframe[i] = new Keyframe(s[i].flame_no * tick_time, s[i].weight);
						//線形補間する
						keyframe[i].tangentMode=TangentModeBothLinear;
 						if(i>0)
						{
							float t=GetLinearTangentForPosition(keyframe[i-1],keyframe[i]);
							keyframe[i-1].outTangent=t;
							keyframe[i].inTangent=t;
 						}
					}
					AddDummyKeyframe(ref keyframe);

					// Z軸移動にキーフレームを打つ
					AnimationCurve curve = new AnimationCurve(keyframe);
					AnimationUtility.SetEditorCurve(clip,"Expression/" + skin.Key,typeof(Transform),"m_LocalPosition.z",curve);
				}
			}
			
			// ボーンのパスを取得する
			string GetBonePath(Transform transform)
			{
				string buf;
				if (transform.parent == null)
					return transform.name;
				else 
					buf = GetBonePath(transform.parent);
				return buf + "/" + transform.name;
			}
			
			// ボーンの子供を再帰的に走査
			void FullSearchBonePath(Transform transform, Dictionary<string, string> dic)
			{
				int count = transform.GetChildCount();
				for (int i = 0; i < count; i++)
				{
					Transform t = transform.GetChild(i);
					FullSearchBonePath(t, dic);
				}
				
				// オブジェクト名が足されてしまうので抜く
				string buf = "";
				string[] spl = GetBonePath(transform).Split('/');
				for (int i = 1; i < spl.Length-1; i++)
					buf += spl[i] + "/";
				buf += spl[spl.Length-1];

				try
				{
					dic.Add(transform.name, buf);
				}
				catch (ArgumentException arg)
				{
					Debug.Log(arg.Message);
					Debug.Log("An element with the same key already exists in the dictionary. -> " + transform.name);
				}

				// dicには全てのボーンの名前, ボーンのパス名が入る
			}
			
			// 無駄なカーブを登録してるけどどうするか
			void FullEntryBoneAnimation(MMD.VMD.VMDFormat format, AnimationClip clip, Dictionary<string, string> dic, Dictionary<string, GameObject> obj, int interpolationQuality)
			{
				foreach (KeyValuePair<string, string> p in dic)	// keyはtransformの名前, valueはパス
				{
					// 互いに名前の一致する場合にRigidbodyが存在するか調べたい
					GameObject current_obj = null;
					if(obj.ContainsKey(p.Key)){
						current_obj = obj[p.Key];
						
						// Rigidbodyがある場合はキーフレの登録を無視する
						var rigid = current_obj.GetComponent<Rigidbody>();
						if (rigid != null && !rigid.isKinematic)
						{
							continue;
						}
					}
					
					// キーフレの登録
					CreateKeysForLocation(format, clip, p.Key, p.Value, interpolationQuality, current_obj);
					CreateKeysForRotation(format, clip, p.Key, p.Value, interpolationQuality);
				}
			}

			// とりあえず再帰的に全てのゲームオブジェクトを取得する
			void GetGameObjects(Dictionary<string, GameObject> obj, GameObject assign_pmd)
			{
				for (int i = 0; i < assign_pmd.transform.childCount; i++)
				{
					var transf = assign_pmd.transform.GetChild(i);
					try
					{
						obj.Add(transf.name, transf.gameObject);
					}
					catch (ArgumentException arg)
					{
						Debug.Log(arg.Message);
						Debug.Log("An element with the same key already exists in the dictionary. -> " + transf.name);
					}

					if (transf == null) continue;		// ストッパー
					GetGameObjects(obj, transf.gameObject);
				}
			}

			// クリップをアニメーションに登録する
			public void CreateAnimationClip(MMD.VMD.VMDFormat format, GameObject assign_pmd, Animation anim, bool create_asset, int interpolationQuality)
			{
				//Animation anim = assign_pmd.GetComponent<Animation>();
				
				// クリップの作成
				AnimationClip clip = new AnimationClip();
				clip.name = format.clip_name;
				
				Dictionary<string, string> bone_path = new Dictionary<string, string>();
				Dictionary<string, GameObject> gameobj = new Dictionary<string, GameObject>();
				GetGameObjects(gameobj, assign_pmd);		// 親ボーン下のGameObjectを取得
				FullSearchBonePath(assign_pmd.transform, bone_path);
				FullEntryBoneAnimation(format, clip, bone_path, gameobj, interpolationQuality);

				CreateKeysForSkin(format, clip);	// 表情の追加
				
				// ここで登録
				//anim.AddClip(clip, format.clip_name);

				if (create_asset)
				{
					// フォルダを生成してアニメーションのファイルを書き出す
					string prefab_folder = AssetDatabase.GetAssetPath(assign_pmd);
					prefab_folder = Path.GetDirectoryName(prefab_folder);

					Debug.Log(prefab_folder);

					if (!Directory.Exists(prefab_folder + "/Animation"))
						AssetDatabase.CreateFolder(prefab_folder, "Animation");

					AssetDatabase.CreateAsset(clip, prefab_folder + "/Animation/" + clip.name + ".anim");
				}
				else
				{
					// こちらはPrefabの中に入れるタイプ
					AssetDatabase.AddObjectToAsset(clip, AssetDatabase.GetAssetPath(assign_pmd));
				}
				
				AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip));
			}
		}
	}
}
#endif