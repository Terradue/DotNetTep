echo "Cleaning..."
rm -rf database
mkdir -p database/core
mkdir -p database/modules/tep/db
echo "Copying from packages contents"
cp -pr ../packages/**/content/core/** database/core
cp -pr ../packages/**/content/modules/** database/modules
cp -pr ../Terradue.Tep/Resources/db/* database/modules/tep/db
echo "All done - ready for tests"
