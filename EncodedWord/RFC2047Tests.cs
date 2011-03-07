namespace EncodedWord
{
    using System;
    using System.Linq;
    using System.Text;

    using Xunit;

    public class RFC2047Tests
    {
        [Fact]
        public void Should_decode_lower_case_q_quoted_text()
        {
            var input = @"=?iso-8859-1?q?=A1Hola,_se=F1or!?=";

            var output = RFC2047.Decode(input);

            Assert.Equal(@"¡Hola, señor!", output);
        }

        [Fact]
        public void Should_decode_upper_case_q_quoted_text()
        {
            var input = @"=?iso-8859-1?Q?=A1Hola,_se=F1or!?=";

            var output = RFC2047.Decode(input);

            Assert.Equal(@"¡Hola, señor!", output);
        }

        [Fact]
        public void Should_decode_upper_case_b_quoted_text()
        {
            var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes("Some test text"));
            var input = String.Format(@"=?iso-8859-1?B?{0}?=", base64String);

            var output = RFC2047.Decode(input);

            Assert.Equal(@"Some test text", output);
        }

        [Fact]
        public void Should_decode_lower_case_b_quoted_text()
        {
            var base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes("Some test text"));
            var input = String.Format(@"=?iso-8859-1?b?{0}?=", base64String);

            var output = RFC2047.Decode(input);

            Assert.Equal(@"Some test text", output);
        }

        [Fact]
        public void Should_decode_multiple_quoted_blocks()
        {
            var input = @"Normal and multiple =?iso-8859-1?Q?=A1Hola,_se=F1or!?= quoted blocks =?iso-8859-1?B?U29tZSB0ZXN0IHRleHQ=?=";

            var output = RFC2047.Decode(input);

            Assert.Equal(@"Normal and multiple ¡Hola, señor! quoted blocks Some test text", output);
        }

        [Fact]
        public void Should_fall_back_to_8859_if_character_set_invalid()
        {
            var input = @"=?wrong?Q?=A1Hola,_se=F1or!?=";

            var output = RFC2047.Decode(input);

            Assert.Equal(@"¡Hola, señor!", output);
        }

        [Fact]
        public void Should_just_return_original_text_if_encoding_type_is_not_recognised()
        {
            var input = @"=?iso-8859-1?Z?=A1Hola,_se=F1or!?=";

            var output = RFC2047.Decode(input);

            Assert.Equal(input, output);
        }

        [Fact]
        public void Should_ignore_invalid_hex_bytes_in_q_encoded_input()
        {
            var input = @"=?iso-8859-1?Q?=Z4Hola,_se=F1or!?=";

            var output = RFC2047.Decode(input);

            Assert.Equal(@"=Z4Hola, señor!", output);
        }

        [Fact]
        public void Should_handle_encoded_words_separated_by_cr_lf_space()
        {
            var input = "=?iso-8859-1?q?=A1Hola,_se=F1or!?=\r\n =?iso-8859-1?q?=A1Hola,_se=F1or!?=";

            var output = RFC2047.Decode(input);

            Assert.Equal(@"¡Hola, señor!¡Hola, señor!", output);
        }

        [Fact]
        public void Should_return_empty_string_when_encoding_empty_string()
        {
            var input = @"";

            var output = RFC2047.Encode("");

            Assert.Equal("", output);
        }

        [Fact]
        public void Should_throw_when_encoding_with_unknown_content_encoding()
        {
            Assert.Throws(typeof(ArgumentException), () => RFC2047.Encode("test", RFC2047.ContentEncoding.Unknown));
        }

        [Fact]
        public void Should_throw_when_encoding_with_invalid_character_set()
        {
            Assert.Throws(typeof(ArgumentException), () => RFC2047.Encode("test", RFC2047.ContentEncoding.QEncoding, "fake"));
        }

        [Fact]
        public void Should_encode_to_b_encoding()
        {
            var inputText = "Some test text";
            var inputCharacterSet = "iso-8859-1";
            var encodingType = RFC2047.ContentEncoding.Base64;

            var result = RFC2047.Encode(inputText, encodingType, inputCharacterSet);

            Assert.Equal("=?iso-8859-1?B?U29tZSB0ZXN0IHRleHQ=?=", result);
        }

        [Fact]
        public void Should_encode_to_q_encoding()
        {
            var inputText = "¡Hola, señor!";
            var inputCharacterSet = "iso-8859-1";
            var encodingType = RFC2047.ContentEncoding.QEncoding;

            var result = RFC2047.Encode(inputText, encodingType, inputCharacterSet);

            Assert.Equal("=?iso-8859-1?Q?=A1Hola=2C_se=F1or!?=", result);
        }

        [Fact]
        public void Should_decode_q_encoded_text_back_to_original_text()
        {
            var inputText = "¡Hola, señor!";
            var inputCharacterSet = "iso-8859-1";
            var encodingType = RFC2047.ContentEncoding.QEncoding;
            var encoded = RFC2047.Encode(inputText, encodingType, inputCharacterSet);

            var result = RFC2047.Decode(encoded);

            Assert.Equal(inputText, result);
        }

        [Fact]
        public void Should_add_separators_so_lines_do_not_exceed_75_characters()
        {
            var inputText = "This is some very long text. It should be split so no line exceeds 75 characters and should have the separator in between";
            var inputCharacterSet = "iso-8859-1";
            var encodingType = RFC2047.ContentEncoding.Base64;

            var result = RFC2047.Encode(inputText, encodingType, inputCharacterSet).Split(new[] { "\r\n " }, StringSplitOptions.RemoveEmptyEntries);

            Assert.False(result.Where(l => l.Length > 75).Any());
        }
    }
}