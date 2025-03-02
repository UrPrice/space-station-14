// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Text;
using System.Text.RegularExpressions;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Language.EncryptionMethods;

/// <summary>
/// Scramble a message depending on its length using a specific list of syllables
/// </summary>
public sealed partial class RandomSyllablesScrambleMethod : ScrambleMethod
{
    /// <summary>
    ///  List of syllables from which the original message will be encrypted
    ///  A null value does not scramlbe the message in any way
    /// </summary>
    [DataField(required: true)]
    public List<string> Syllables = new();

    /// <summary>
    ///  Chance of space between scrambled syllables
    /// </summary>
    [DataField]
    public float SpaceChance = 0.5f;

    /// <summary>
    ///  Chance for a dot after a scrambled syllable.
    /// </summary>
    [DataField]
    public float DotChance = 0.3f;

    /// <summary>
    /// Chance of the <see cref="SpecialCharacter"/> after a scrambled syllable
    /// </summary>
    [DataField]
    public float SpecialCharacterChance = 0.3f;

    [DataField]
    public string SpecialCharacter = string.Empty;

    public override string ScrambleMessage(string message, int? seed = null)
    {
        if (Syllables.Count == 0)
            return message;

        var wordRegex = @"\S+";
        var matches = Regex.Matches(message, wordRegex);
        if (matches.Count <= 0)
            return message;

        var result = new StringBuilder();
        foreach (Match m in matches)
        {
            string scrambledWord;
            if (seed != null)
            {
                foreach (var c in m.Value.ToCharArray())
                {
                    seed += c;
                }
                scrambledWord = ScrambleWithSeed(m.Value, seed.Value);
            }
            else
            {
                scrambledWord = ScrambleWithoutSeed(message);
            }

            result.Append(scrambledWord);
        }

        var punctuation = ExtractPunctuation(message);
        result.Append(punctuation);

        return result.ToString();
    }

    private string ScrambleWithoutSeed(string message)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        var encryptedMessage = new StringBuilder();
        var capitalize = false;
        while (encryptedMessage.Length < message.Length)
        {
            var curSyllable = random.Pick(Syllables);

            if (capitalize)
            {
                curSyllable = string.Concat(curSyllable.Substring(0, 1).ToUpper(), curSyllable.AsSpan(1));
                capitalize = false;
            }
            encryptedMessage.Append(curSyllable);

            if (random.Prob(SpecialCharacterChance))
            {
                encryptedMessage.Append(SpecialCharacter);
            }
            else if (random.Prob(DotChance))
            {
                encryptedMessage.Append(". ");
                capitalize = true;
            }
            else if (random.Prob(SpaceChance))
            {
                encryptedMessage.Append(' ');
            }
        }

        var result = encryptedMessage.ToString().Trim();
        return result;
    }

    private string ScrambleWithSeed(string message, int seed)
    {
        var random = new System.Random();

        var encryptedMessage = new StringBuilder();
        var capitalize = false;
        while (encryptedMessage.Length < message.Length)
        {
            var curSyllable = random.Pick(Syllables);

            if (capitalize)
            {
                curSyllable = curSyllable.Substring(0, 1).ToUpper() + curSyllable.Substring(1);
                capitalize = false;
            }
            encryptedMessage.Append(curSyllable);

            if (random.Prob(SpecialCharacterChance))
            {
                encryptedMessage.Append(SpecialCharacter);
            }
            else if (random.Prob(DotChance))
            {
                encryptedMessage.Append(". ");
                capitalize = true;
            }
            else if (random.Prob(SpaceChance))
            {
                encryptedMessage.Append(' ');
            }
        }

        var result = encryptedMessage.ToString().Trim();
        return result;
    }

    /// <summary>
    ///     Takes the last punctuation out of the original post
    ///     (Does not affect the internal punctuation of the sentence)
    /// </summary>
    private static string ExtractPunctuation(string input)
    {
        var punctuationBuilder = new StringBuilder();
        for (var i = input.Length - 1; i >= 0; i--)
        {
            if (char.IsPunctuation(input[i]))
                punctuationBuilder.Insert(0, input[i]);
            else
                break;
        }
        punctuationBuilder.Append(' '); // save whitespace before language tag

        return punctuationBuilder.ToString();
    }
}
