namespace EncodedWord
{
    using System;
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
    }
}