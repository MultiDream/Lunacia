﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpecialText : MonoBehaviour
{
    /*
     * Important:
     * This script doesn't work with nested tags and requires openening <tags> to 
     * be matched with closing </tags>
     * 
     * When implementing tags you may need to wait one frame before doing stuff
     * in each coroutine
     */

    private TextMeshProUGUI m_TextMeshPro;
    private static string[] commands = {
        "jitter",
        "jittery",
        "shake",
        "shaky",
        "wave",
        "wavy"
    };
    private static int[] commandHashes;
    private List<CommandArg> commandArgs = new List<CommandArg>();

    // subscribe to TMP event manager for when text updates
    //bool hasTextChanged; 

    // idk how to best do settings
    public float jitterMultiplier = 1;
    public float shakeMultiplier = 1;
    public float waveAmplitude = 1;
    public float wavePeriod = 1;


    private void Awake()
    {
        if (commandHashes == null)
        {
            commandHashes = new int[commands.Length];
            for (int i = 0; i < commands.Length; i++)
            {
                commandHashes[i] = commands[i].GetHashCode();
                //Debug.Log("Hash for '" + commands[i] + "': " + commandHashes[i]);
            }
        }


        // this is in awake now because `m_TextMeshPro.text = text;` updates the mesh
        m_TextMeshPro = GetComponent<TextMeshProUGUI>();
        if (m_TextMeshPro == null)
        {
            Debug.LogWarning("TextMeshPro Script not on object!");
        }
        StartCoroutine(ParseText(m_TextMeshPro.text));
    }

    void Start()
    {

    }

    private struct CommandArg
    {
        public string command;
        public int hash;
        public int start;
        public int end;

        public CommandArg(string c, int h, int s, int e)
        {
            command = c;
            hash = h;
            start = s;
            end = e;
        }
    }

    private IEnumerator ParseText(string text)
    {
        int offset = 0;

        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '<')
            {
                // find if closing > exists
                int first_right_chev = text.IndexOf('>', i);
                if (first_right_chev == -1)
                {
                    i++;
                    continue;
                }

                // find command between the < >
                string command = text.Substring(i + 1, first_right_chev - i - 1);
                int commandHash = command.GetHashCode();
                if (!IsValid(commandHash))
                {
                    // lazy color solution
                    if (command[0] == '#')
                    {
                        offset += 9;
                        i += 8;
                    } else if (command.Equals("/color"))
                    {
                        offset += 8;
                        i += 7;
                    } else
                    {
                        i++;
                    }
                    continue;
                } 

                // find if closing </ > exists
                int closing_tag = text.IndexOf("</" + command + ">", i);
                if (closing_tag == -1)
                {
                    i++;
                    continue;
                }

                // remove tags
                text = text.Substring(0, i) + 
                       text.Substring(i + command.Length + 2, closing_tag - (i + command.Length + 2)) + 
                       text.Substring(closing_tag + command.Length + 3);
                m_TextMeshPro.text = text;

                //Debug.Log(command + " " + i + " - " + (closing_tag - command.Length - 2));
                //ParseCommand(commandHash, i - offset, closing_tag - command.Length - 3 - offset);
                commandArgs.Add(new CommandArg(command, commandHash, i - offset, closing_tag - command.Length - 3 - offset));
            }

            i++;
        }

        yield return null;

        foreach (CommandArg c in commandArgs)
        {
            ParseCommand(c.hash, c.start, c.end);
        }
    }

    private bool IsValid(int hash)
    {
        foreach (int h in commandHashes)
        {
            if (h == hash)
                return true;
        }
        return false;
    }

    private bool IsValid(string command)
    {
        foreach (string s in commands)
        {
            if (command.Equals(s))
                return true;
        }
        return false;
    }

    /*
     * end is inclusive, so for example:
     * <tag>My text</tag>
     * would be ParseCommand(hash, 0, 7);
     */
    private bool ParseCommand(int commandHash, int start, int end)
    {
        switch (commandHash)
        {
            // jitter
            case -1623808880:
            // jittery
            case 270799001:
                StartCoroutine(TMPJitter(start, end, jitterMultiplier * 2, jitterMultiplier * 2));
                break;
            // shake
            case 371760912:
            // shaky
            case 371760908:
                StartCoroutine(TMPJitter(start, end, 0, shakeMultiplier * 10));
                break;
            // wave
            case -1966748055:
            // wavy
            case -1066070667:
                StartCoroutine(Wavy(start, end, waveAmplitude * 5, wavePeriod * .5f));
                break;
            default:
                return false;
        }
        return true;
    }


    #region jitter-shaky

    // shamelessly stolen from TMPro's example, VertexJitter.cs

    private struct VertexAnim
    {
        public float angleRange;
        public float angle;
        public float speed;
    }

    IEnumerator TMPJitter(int start, int end, float AngleMultiplier, float CurveScale)
    {

        bool hasTextChanged;

        // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
        // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
        //m_TextMeshPro.ForceMeshUpdate();
        yield return null;

        TMP_TextInfo textInfo = m_TextMeshPro.textInfo;

        Matrix4x4 matrix;

        int loopCount = 0;
        hasTextChanged = true;

        // Create an Array which contains pre-computed Angle Ranges and Speeds for a bunch of characters.
        VertexAnim[] vertexAnim = new VertexAnim[1024];
        for (int i = 0; i < 1024; i++)
        {
            vertexAnim[i].angleRange = Random.Range(10f, 25f);
            vertexAnim[i].speed = Random.Range(1f, 3f);
        }

        // Cache the vertex data of the text object as the Jitter FX is applied to the original position of the characters.
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        while (true)
        {
            // Get new copy of vertex data if the text has changed.
            if (hasTextChanged)
            {
                // Update the copy of the vertex data for the text object.
                cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

                hasTextChanged = false;
            }

            int characterCount = textInfo.characterCount;

            // If No Characters then just yield and wait for some text to be added
            if (characterCount < end - start)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }


            for (int i = start; i <= end; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // Skip characters that are not visible and thus have no geometry to manipulate.
                if (!charInfo.isVisible)
                    continue;

                // Retrieve the pre-computed animation data for the given character.
                VertexAnim vertAnim = vertexAnim[i];

                // Get the index of the material used by the current character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                // Get the index of the first vertex used by this text element.
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                // Get the cached vertices of the mesh used by this text element (character or sprite).
                Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                // Determine the center point of each character at the baseline.
                //Vector2 charMidBasline = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                // Determine the center point of each character.
                Vector2 charMidBasline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                // This is needed so the matrix TRS is applied at the origin for each character.
                Vector3 offset = charMidBasline;

                Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                vertAnim.angle = Mathf.SmoothStep(-vertAnim.angleRange, vertAnim.angleRange, Mathf.PingPong(loopCount / 25f * vertAnim.speed, 1f));
                Vector3 jitterOffset = new Vector3(Random.Range(-.25f, .25f), Random.Range(-.25f, .25f), 0);

                matrix = Matrix4x4.TRS(jitterOffset * CurveScale, Quaternion.Euler(0, 0, Random.Range(-5f, 5f) * AngleMultiplier), Vector3.one);

                destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                destinationVertices[vertexIndex + 0] += offset;
                destinationVertices[vertexIndex + 1] += offset;
                destinationVertices[vertexIndex + 2] += offset;
                destinationVertices[vertexIndex + 3] += offset;

                vertexAnim[i] = vertAnim;
            }

            // Push changes into meshes
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                m_TextMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            loopCount += 1;

            yield return new WaitForSeconds(0.1f);
        }
    }
    #endregion

    #region wavy


    IEnumerator Wavy(int start, int end, float amplitude, float periodMultiplier)
    {

        bool hasTextChanged;

        // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
        // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
        //m_TextMeshPro.ForceMeshUpdate();
        yield return null;

        TMP_TextInfo textInfo = m_TextMeshPro.textInfo;

        int loopCount = 0;
        hasTextChanged = true;

        // Cache the vertex data of the text object as the Jitter FX is applied to the original position of the characters.
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        while (true)
        {
            // Get new copy of vertex data if the text has changed.
            if (hasTextChanged)
            {
                // Update the copy of the vertex data for the text object.
                cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

                hasTextChanged = false;
            }

            int characterCount = textInfo.characterCount;

            // If No Characters then just yield and wait for some text to be added
            if (characterCount < end - start)
            {
                yield return new WaitForSeconds(0.25f);
                continue;
            }


            for (int i = start; i <= end; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // Skip characters that are not visible and thus have no geometry to manipulate.
                if (!charInfo.isVisible)
                    continue;

                // Get the index of the material used by the current character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                // Get the index of the first vertex used by this text element.
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                // Get the cached vertices of the mesh used by this text element (character or sprite).
                Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;


                // i dont know if this is the best way to do it or not lol
                Vector3 offset = Vector3.up * amplitude * Mathf.Sin(periodMultiplier * (loopCount - i));

                Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] + offset;
                destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] + offset;
                destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] + offset;
                destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] + offset;
            }

            // Push changes into meshes
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                m_TextMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            loopCount += 1;

            // changed from 0.1f
            yield return new WaitForSeconds(0.05f);
        }
    }
    #endregion

}