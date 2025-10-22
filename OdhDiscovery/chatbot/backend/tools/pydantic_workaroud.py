from ast import literal_eval
import json

## TODO: we should use pydantic tools, structured tools and pass to llm tools schema
def _parse_json_string(value):
    if isinstance(value, (list, dict)):
        return value
    try:
        return literal_eval(value)
    except:
        return []
    
    """Parse JSON string to Python object if needed"""
    if isinstance(value, str) and (value.startswith('[') or value.startswith('{')):
        try:
            return json.loads(value)
        except json.JSONDecodeError:
            logger.warning(f"Failed to parse JSON string: {value}")
            return value
    return value