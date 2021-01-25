### 1.0.0 - 2021-01-25
* breaking: various type changes (use more accurate terms, namespace changes)
* breaking: the `Parse` member throws an exception when parsing fails, does not return a DU
* TFM: remove net461 target, only target netstandard2.0
* parsing: do not accept unlisted/non-internet TLDs
* parsing: better handling of mixed-case input
* parsing: accept URLs as input
* lazy-load the public suffix rules
* update URL of the rule list, per guidelines

### 0.1.0 - 2018-07-08
* Initial release
* Parse and validate hostnames
* Extract domain parts
