"""
Content API Client
Clean, configurable client for ODH Content API
"""
import httpx
import logging
from typing import Any
from config import settings

logger = logging.getLogger(__name__)


class ContentAPIClient:
    """
    Client for ODH Content API
    Designed to be easily configurable and adaptable to API changes
    """

    def __init__(
        self,
        base_url: str | None = None,
        timeout: int | None = None
    ):
        """
        Initialize Content API client

        Args:
            base_url: Override default base URL from settings
            timeout: Override default timeout from settings
        """
        self.base_url = base_url or settings.content_api_base_url
        self.timeout = timeout or settings.api_timeout
        logger.info(f"ContentAPIClient initialized with base_url: {self.base_url}")

    async def get_datasets(self) -> list[dict]:
        """
        Get list of all available datasets from MetaData API (handles pagination)

        Fetches from /MetaData endpoint which contains complete metadata about
        all available datasets including Shortname, ApiUrl, ApiDescription, etc.

        Returns:
            List of dataset metadata objects with fields:
            - Shortname: Human-readable dataset name
            - ApiUrl: Complete API URL with default filters
            - ApiDescription: Descriptions in multiple languages
            - ApiType: 'content' or 'timeseries'
            - Dataspace: Category (tourism, mobility, weather, etc.)
            - ApiFilter: Default filters as array
            - PathParam: API path components
            - BaseUrl: Base API URL
        """
        try:
            all_datasets = []
            page = 1

            async with httpx.AsyncClient(timeout=self.timeout) as client:
                while True:
                    # Fetch current page from MetaData endpoint
                    params = {
                        "pagenumber": page,
                        "pagesize": 100,
                        "removenullvalues": True  # Clean response
                    }
                    metadata_url = f"{self.base_url}/MetaData"
                    response = await client.get(metadata_url, params=params)
                    response.raise_for_status()
                    data = response.json()

                    # Add items from this page
                    items = data.get("Items", [])
                    # Filter out deprecated datasets
                    active_items = [item for item in items if not item.get("Deprecated", False)]
                    all_datasets.extend(active_items)

                    # Check if there are more pages
                    current_page = data.get("CurrentPage", page)
                    total_pages = data.get("TotalPages", 1)

                    logger.info(f"Fetched datasets page {current_page}/{total_pages} ({len(active_items)} active items)")

                    if current_page >= total_pages:
                        break

                    page += 1

            logger.info(f"Fetched total of {len(all_datasets)} datasets from MetaData API")
            return all_datasets

        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch datasets from MetaData API: {e}")
            raise

    async def get_dataset_entries(
        self,
        dataset_name: str,
        page: int = 1,
        pagesize: int | None = None,
        raw_filter: str | None = None,
        raw_sort: str | None = None,
        fields: list[str] | None = None,
        language: str | None = None,
        search_filter: str | None = None,
    ) -> dict[str, Any]:
        """
        Get entries from a specific dataset

        Args:
            dataset_name: Name of the dataset (e.g., 'activity', 'accommodation')
            page: Page number (1-indexed)
            pagesize: Number of entries per page
            raw_filter: Raw filter expression (e.g., "Active eq true")
            raw_sort: Raw sort expression (e.g., "Shortname asc")
            fields: List of fields to include in response
            language: Language filter (e.g., 'en', 'de', 'it')
            search_filter: Full-text search query

        Returns:
            Dictionary with 'Items', 'TotalResults', 'TotalPages', etc.
        """
        pagesize = pagesize or settings.default_page_size
        pagesize = min(pagesize, settings.max_page_size)

        params = {
            "pagenumber": page,
            "pagesize": pagesize,
        }

        if raw_filter:
            params["rawfilter"] = raw_filter
        if raw_sort:
            params["rawsort"] = raw_sort
        if fields:
            params["fields"] = ",".join(fields)
        if language:
            params["language"] = language
        if search_filter:
            params["searchfilter"] = search_filter

        url = f"{self.base_url}/MetaData?searchfilter={dataset_name}"

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                logger.debug(f"Fetching dataset metadata: {url}")
                response = await client.get(url)
                response.raise_for_status()
                metadata = response.json()
                
                logger.debug(f"Fetching dataset entries: {metadata['Items'][0]['ApiUrl']} with params: {params}")
                entries = await client.get(url=metadata['Items'][0]['ApiUrl'], params=params)
                return entries.json()

        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch dataset entries for {dataset_name}: {e}")
            raise

    async def get_entry_by_id(
        self,
        dataset_name: str,
        entry_id: str,
        fields: list[str] | None = None,
    ) -> dict[str, Any]:
        """
        Get a single entry by ID

        Args:
            dataset_name: Name of the dataset
            entry_id: Entry ID
            fields: List of fields to include in response

        Returns:
            Single entry object
        """
        params = {}
        if fields:
            params["fields"] = ",".join(fields)

        url = f"{self.base_url}/{dataset_name}/{entry_id}"

        try:
            async with httpx.AsyncClient(timeout=self.timeout) as client:
                logger.debug(f"Fetching entry by ID: {url}")
                response = await client.get(url, params=params)
                response.raise_for_status()
                return response.json()
        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch entry {entry_id} from {dataset_name}: {e}")
            raise

    async def count_entries(
        self,
        dataset_name: str,
        raw_filter: str | None = None,
    ) -> int:
        """
        Count entries in a dataset (with optional filter)

        Args:
            dataset_name: Name of the dataset
            raw_filter: Raw filter expression

        Returns:
            Total count of entries
        """
        # Fetch with pagesize=1 to get total count efficiently
        result = await self.get_dataset_entries(
            dataset_name=dataset_name,
            page=1,
            pagesize=1,
            raw_filter=raw_filter
        )
        return result.get("TotalResults", 0)

    async def get_metadata_by_shortname(self, shortname: str) -> dict | None:
        """
        Get metadata for a specific dataset by its Shortname

        Args:
            shortname: The Shortname of the dataset (e.g., "Accommodation", "ODHActivityPoi")

        Returns:
            Metadata object for the dataset, or None if not found
        """
        try:
            # Fetch all metadata (cached in production)
            all_metadata = await self.get_datasets()

            # Find by Shortname (case-sensitive)
            for metadata in all_metadata:
                if metadata.get("Shortname") == shortname:
                    logger.info(f"Found metadata for dataset: {shortname}")
                    return metadata

            logger.warning(f"No metadata found for dataset: {shortname}")
            return None

        except httpx.HTTPError as e:
            logger.error(f"Failed to fetch metadata for {shortname}: {e}")
            raise
